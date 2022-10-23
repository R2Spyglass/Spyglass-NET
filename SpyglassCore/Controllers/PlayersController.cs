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

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("lookup_uid")]
        public IActionResult GetPlayerByUID(string uid)
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

            var foundPlayer = _context.Players
                .Where(p => p.UniqueID == uid)
                .Include(p => p.Aliases)
                .FirstOrDefault();

            var result = new PlayerSearchResult
            {
                Success = foundPlayer != null,
                UniqueID = uid,
                Error = foundPlayer == null ? $"Could not find a player with uid '{uid}' in the database." : null,
                Matches = foundPlayer != null ? new List<PlayerInfo> {foundPlayer} : new List<PlayerInfo>()
            };

            return Ok(result);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("lookup_name")]
        public IActionResult GetPlayerByUsername(string username)
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
    }
}