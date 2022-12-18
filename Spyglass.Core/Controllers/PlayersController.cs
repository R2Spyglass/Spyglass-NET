using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Spyglass.Core.Database;
using Spyglass.Models;
using SpyglassNET.Utilities;

namespace SpyglassNET.Controllers
{
    [ApiController]
    [Route("players/")]
    public class PlayersController : Controller
    {
        private readonly SpyglassContext _context;

        public PlayersController(SpyglassContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Searches for a player with the given uid. 
        /// </summary>
        /// <param name="uid"> The unique id to search for. </param>
        /// <param name="includeAliases"> Whether or not to include the aliases of the player. </param>
        /// <param name="includeSanctions"> Whether or not to include the sanctions of the player. </param>
        /// <returns> A PlayerSearchResult containing information about the player on success, or an error message on failure. </returns>
        [HttpGet("lookup_uid")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<PlayerSearchResult> GetPlayerByUID(string uid, bool includeAliases = true, bool includeSanctions = false)
        {
            if (!SpyglassUtils.ValidateUniqueId(uid))
            {
                return Ok(new PlayerSearchResult
                {
                    Success = false,
                    UniqueID = uid,
                    Error = "Invalid UID specified."
                });
            }

            var playerQuery = _context.Players
                .Where(p => p.UniqueID == uid);

            if (includeAliases)
            {
                playerQuery = playerQuery.Include(p => p.Aliases);
            }

            if (includeSanctions)
            {
                playerQuery = playerQuery.Include(p => p.Sanctions);
            }
                
            var foundPlayer = playerQuery.FirstOrDefault();

            if (foundPlayer != null)
            {
                return Ok(new PlayerSearchResult
                {
                    Success = true,
                    UniqueID = uid,
                    Matches = new List<PlayerInfo> { foundPlayer }
                });
            }

            return Ok(new PlayerSearchResult
            {
                Success = false,
                Error = $"Could not find a player with uid '{uid}' in the database."
            });
        }

        /// <summary>
        /// Searches for players with the given username, whether exact or partial.
        /// Will also check for their previous usernames. 
        /// </summary>
        /// <param name="username"> The username to search for. </param>
        /// <returns> A PlayerSearchResult containing information about the player on success, or an error message on failure. </returns>
        [HttpGet("lookup_name")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<PlayerSearchResult> GetPlayerByUsername(string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return Ok(
                    new PlayerSearchResult
                    {
                        Success = false,
                        Username = username,
                        Error = "Cannot lookup a player with an empty username."
                    });
            }

            if (username.Length > 64)
            {
                return Ok(
                    new PlayerSearchResult
                    {
                        Success = false,
                        Username = username,
                        Error = "Cannot lookup a player with a username longer than 64 characters."
                    });
            }

            var matchUsername = username.ToLower();
            var matches = _context.Players
                .Include(p => p.Aliases)
                .Where(p =>
                    p.Username.ToLower().Contains(matchUsername)
                    || p.Aliases.Any(a => a.Alias.ToLower().Contains(matchUsername)))
                .ToList();

            var success = matches.Count != 0;
            return Ok(new PlayerSearchResult
            {
                Success = success,
                Error = success ? null : $"Could not find any player that matches username '{username}'.",
                Username = username,
                Matches = matches
            });
        }

        /// <summary>
        /// Tracks the given players UIDs with their usernames at the time, creating or updating player info in the database.
        /// Scopes: trusted_server, players
        /// </summary>
        /// <returns> Whether or not tracking was a success. </returns>
        [HttpPost("track_players")]
        [Authorize("trusted_server")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<ApiResult> TrackPlayers(PlayerTrackingData trackingData)
        {
            if (string.IsNullOrWhiteSpace(trackingData.Hostname))
            {
                return Ok(ApiResult.FromError("Endpoint 'track_players' called with a null or whitespace 'hostname' query parameter."));
            }

            if (trackingData.Hostname.Length > 128)
            {
                return Ok(ApiResult.FromError("Endpoint 'track_players' called with too long of a hostname (128 characters max)."));
            }

            if (trackingData.Players.Count == 0)
            {
                return Ok(ApiResult.FromSuccess());
            }

            if (trackingData.Players.Count > 64)
            {
                return Ok(ApiResult.FromError("Cannot use endpoint 'track_players' with more than 64 players at a time."));
            }

            trackingData.Hostname = trackingData.Hostname.Trim();
            
            // Ensure we're only holding valid player identities, as best as we can.
            var invalidPlayers = trackingData.Players
                .Where(p => !SpyglassUtils.ValidateUniqueId(p.UniqueID) || string.IsNullOrWhiteSpace(p.Username) || p.Username.Length > 64)
                .ToList();

            if (invalidPlayers.Any())
            {
                return Ok(ApiResult.FromError("Endpoint 'track_players' called with one or more invalid player identities in the request."));
            }

            // Sanitize input to remove any trailing whitespace.
            var sanitizedPlayers = trackingData.Players
                .Select(p => new PlayerIdentity {UniqueID = p.UniqueID.Trim(), Username = p.Username.Trim()})
                .ToList();

            var trackedIds = sanitizedPlayers
                .Select(s => s.UniqueID)
                .ToList();

            // Update the last seen time of players and their aliases.
            var knownPlayers = _context.Players
                .Where(p => trackedIds.Contains(p.UniqueID))
                .Include(p => p.Aliases);

            foreach (var trackedPlayer in knownPlayers)
            {
                trackedPlayer.LastSeenAt = DateTimeOffset.UtcNow;
                trackedPlayer.LastSeenOnServer = trackingData.Hostname;

                foreach (var newPlayer in sanitizedPlayers)
                {
                    if (trackedPlayer.UniqueID == newPlayer.UniqueID
                        && trackedPlayer.Username != newPlayer.Username
                        && trackedPlayer.Aliases.All(a => a.Alias != newPlayer.Username))
                    {
                        _context.PlayerAliases.Add(new PlayerAlias { UniqueID = newPlayer.UniqueID, Alias = newPlayer.Username });
                        break;
                    }
                }
            }
            
            // Add the new players that need to be tracked.
            var newPlayers = sanitizedPlayers
                .Where(s => knownPlayers.All(k => k.UniqueID != s.UniqueID))
                .Select(s => new PlayerInfo { UniqueID = s.UniqueID, Username = s.Username, LastSeenOnServer = trackingData.Hostname });

            _context.Players.AddRange(newPlayers);
            _context.SaveChanges();

            return Ok(ApiResult.FromSuccess());
        }

        /// <summary>
        /// Add a new player to the tracked player list.
        /// </summary>
        /// <param name="uniqueId"> The unique id of the player to add. </param>
        /// <param name="username"> The username of the player to add. </param>
        /// <param name="isMaintainer"> Whether or not the player is a maintainer. </param>
        /// <returns> Whether or not adding the player was a success. </returns>
        [HttpPost("add_player")]
        [Authorize("players")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<ApiResult> AddPlayer(string uniqueId, string username, bool isMaintainer = false)
        {
            if (string.IsNullOrWhiteSpace(uniqueId) || !SpyglassUtils.ValidateUniqueId(uniqueId))
            {
                return Ok(ApiResult.FromError("Cannot add a player with an invalid 'uniqueId' parameter."));
            }

            if (string.IsNullOrWhiteSpace(username))
            {
                return Ok(ApiResult.FromError("Cannot add a player with an invalid 'username' parameter."));
            }

            uniqueId = uniqueId.Trim();
            if (_context.Players.Any(p => p.UniqueID == uniqueId))
            {
                return Ok(ApiResult.FromError($"A player with UID '{uniqueId}' already exists."));
            }

            _context.Players.Add(new PlayerInfo {UniqueID = uniqueId, Username = username, IsMaintainer = isMaintainer});
            _context.SaveChangesAsync();
            return Ok(ApiResult.FromSuccess());
        }

        /// <summary>
        /// Changes the maintainer status of a player.
        /// </summary>
        /// <param name="uniqueId"> The unique id of the player to set the maintainer status for. </param>
        /// <param name="isMaintainer"> Whether or not the player is a maintainer or not. </param>
        /// <returns> Whether or not setting the maintainer status of the player was a success. </returns>
        [HttpPost("set_maintainer")]
        [Authorize("admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<ApiResult> SetMaintainerStatus(string uniqueId, bool isMaintainer)
        {
            if (string.IsNullOrWhiteSpace(uniqueId) || !SpyglassUtils.ValidateUniqueId(uniqueId))
            {
                return Ok(ApiResult.FromError("Cannot set maintainer status of a player with an invalid 'uniqueId' parameter."));
            }

            uniqueId = uniqueId.Trim();
            var player = _context.Players.FirstOrDefault(p => p.UniqueID == uniqueId);
            if (player == null)
            {
                return Ok(ApiResult.FromError($"Cannot set maintainer status of unknown player '{uniqueId}'."));
            }

            player.IsMaintainer = isMaintainer;
            _context.SaveChanges();
            return Ok(ApiResult.FromSuccess());
        }
    }
}