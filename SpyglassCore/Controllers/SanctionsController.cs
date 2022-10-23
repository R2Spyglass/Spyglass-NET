using Microsoft.AspNetCore.Mvc;
using Spyglass.Core.Database;
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

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [Route("lookup_uid")]
        public IActionResult LookupSanctionsForUniqueIds([FromQuery] string?[] uids, bool excludeMaintainers = false, bool withExpired = true)
        {
            var sanitized = uids
                .Where(u => u != null)
                .Select(u => u!)
                .DistinctBy(u => u.Trim())
                .ToList();
            
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

            var sanctions = _context.Sanctions
                .Where(s => sanitized.Contains(s.UniqueId) && (withExpired || s.ExpiresAt == null || s.ExpiresAt > DateTimeOffset.UtcNow))
                .Where(s => !excludeMaintainers || !s.OwningPlayer.IsMaintainer)
                .AsEnumerable()
                .GroupBy(s => s.UniqueId)
                .ToDictionary(s => s.Key, s => s.ToList());
            
            var searchResult = new SanctionSearchResult
            {
                Success = true,
                UniqueIDs = sanitized,
                Matches = sanctions,
            };

            return Ok(searchResult);
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("get_by_id")]
        public IActionResult GetSanctionById(int id)
        {
            var sanction = _context.Sanctions.FirstOrDefault(s => s.Id == id);

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
    }
}