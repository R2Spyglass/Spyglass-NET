using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Spyglass.Core.Database;
using Spyglass.Identity;
using Spyglass.Models;
using SpyglassNET.Utilities;

namespace SpyglassNET.Controllers
{
    [ApiController]
    [Route("sanctions")]
    public class SanctionsController : Controller
    {
        private readonly SpyglassContext _context;

        public SanctionsController(SpyglassContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Returns the sanctions issued to the given unique ids, if any.
        /// </summary>
        /// <param name="uids"> The unique ids (UIDs) to lookup sanctions for. </param>
        /// <param name="excludeMaintainers"> Whether or not maintainers (Spyglass admins) should be excluded from the results. Defaults to false. </param>
        /// <param name="withExpired"> Whether or not to include expired sanctions. Defaults to true. </param>
        /// <param name="withPlayerInfo"> Whether or not to include player info in the sanctions. Defaults to false. </param>.
        /// <returns> A SanctionSearchResult with the found sanctions on success, or an error message on failure. </returns>
        [HttpGet("lookup_uid")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<SanctionSearchResult> LookupSanctionsForUniqueIds([FromQuery] string?[] uids, bool excludeMaintainers = false, bool withExpired = true,
            bool withPlayerInfo = false)
        {
            var sanitized = uids
                .Where(u => u != null)
                .Select(u => u!)
                .DistinctBy(u => u.Trim())
                .ToList();

            if (sanitized.Count > 64)
            {
                return Ok(new SanctionSearchResult
                {
                    Success = false,
                    Error = "Cannot lookup more than 64 UIDs for sanctions at a time."
                });
            }
            
            if (sanitized.Count == 0)
            {
                return Ok(new SanctionSearchResult
                {
                    Success = true,
                    UniqueIDs = sanitized,
                    Matches = new Dictionary<string, List<PlayerSanction>>()
                });
            }

            var invalidUniqueId = sanitized.FirstOrDefault(u => !SpyglassUtils.ValidateUniqueId(u));

            if (invalidUniqueId != null)
            {
                return Ok(new SanctionSearchResult
                {
                    Success = false,
                    UniqueIDs = sanitized,
                    Error = $"Cannot check sanctions of invalid uid '{invalidUniqueId}'."
                });
            }

            Dictionary<string, List<PlayerSanction>> sanctions;

            if (withPlayerInfo)
            {
                sanctions = _context.Sanctions
                    .AsNoTracking()
                    .Where(s => sanitized.Contains(s.UniqueId) && (withExpired || s.ExpiresAt == null || s.ExpiresAt > DateTimeOffset.UtcNow))
                    .Where(s => !excludeMaintainers || !s.OwningPlayer.IsMaintainer)
                    .Include(s => s.IssuerInfo)
                    .AsEnumerable()
                    .GroupBy(s => s.UniqueId)
                    .ToDictionary(s => s.Key, s => s.ToList());
            }
            else
            {
                sanctions = _context.Sanctions
                    .AsNoTracking()
                    .Where(s => sanitized.Contains(s.UniqueId) && (withExpired || s.ExpiresAt == null || s.ExpiresAt > DateTimeOffset.UtcNow))
                    .Where(s => !excludeMaintainers || !s.OwningPlayer.IsMaintainer)
                    .AsEnumerable()
                    .GroupBy(s => s.UniqueId)
                    .ToDictionary(s => s.Key, s => s.ToList());
                
                foreach (var pair in sanctions)
                {
                    foreach (var sanction in pair.Value)
                    {
                        sanction.OwningPlayer = null!;
                    }
                }
            }

            var searchResult = new SanctionSearchResult
            {
                Success = true,
                UniqueIDs = sanitized,
                Matches = sanctions,
            };

            return Ok(searchResult);
        }

        /// <summary>
        /// Returns the sanction with the given id, if it exists.
        /// </summary>
        /// <param name="id"> The id of the sanction to retrieve. </param>
        /// <returns> A SanctionSearchResult containing the sanction on success, or an error message on failure. </returns>
        [HttpGet("get_by_id")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<SanctionSearchResult> GetSanctionById(int id)
        {
            var sanction = _context.Sanctions
                .Where(s => s.Id == id)
                .Include(s => s.OwningPlayer)
                .Include(s => s.IssuerInfo)
                .FirstOrDefault();

            var success = sanction != null;
            var searchResult = new SanctionSearchResult
            {
                Success = success,
                Error = !success ? $"Could not find a sanction with id #{id}." : null,
                Matches = success ? new Dictionary<string, List<PlayerSanction>> {{ sanction!.UniqueId, new List<PlayerSanction> { sanction }}} : null,
                Id = id
            };

            return Ok(searchResult);
        }

        /// <summary>
        /// Adds a new sanction to the database.
        /// </summary>
        /// <param name="data"> The sanction data to build the sanction from. </param>
        /// <returns> The sanction that was added on success, or an error message. </returns>
        [HttpPost("add_sanction")]
        [Authorize("sanctions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<SanctionIssueResult> IssueSanctionToPlayer(SanctionIssueData data)
        {
            if (!data.IsValid(out var errorMessage))
            {
                return Ok(new SanctionIssueResult
                {
                    Success = false,
                    Error = $"Could not issue sanction to player due to invalid data: {errorMessage}"
                });
            }

            var expiry = data.ExpiresAt;
            if (data.ExpiresIn != null)
            {
                expiry = DateTimeOffset.UtcNow + TimeSpan.FromMinutes((double)data.ExpiresIn);
            }

            var target = _context.Players
                .AsNoTracking()
                .FirstOrDefault(p => p.UniqueID == data.UniqueId);

            if (target == null)
            {
                if (!string.IsNullOrWhiteSpace(data.Username))
                {
                    var newPlayer = new PlayerInfo
                    {
                        Username = data.Username,
                        UniqueID = data.UniqueId
                    };

                    _context.Players.Add(newPlayer);
                    _context.SaveChanges();
                }
                else
                {
                    return Ok(new SanctionIssueResult
                    {
                        Success = false,
                        Error = $"Cannot add sanction to unknown player '{data.UniqueId}', please provide a username."
                    });
                }
            }

            var sanction = new PlayerSanction
            {
                UniqueId = data.UniqueId,
                IssuerId = data.IssuerId,
                ExpiresAt = expiry,
                Reason = data.Reason,
                Type = data.Type,
                PunishmentType = data.PunishmentType
            };

            try
            {
                _context.Sanctions.Add(sanction);
                _context.SaveChanges();

                return Ok(new SanctionIssueResult
                {
                    Success = true,
                    IssuedSanction = sanction
                });
            }
            catch (Exception ex)
            {
                return Ok(new SanctionIssueResult
                {
                    Success = false,
                    Error = $"Failed to add sanction to player: {ex.Message}."
                });
            }
        }
    }
}