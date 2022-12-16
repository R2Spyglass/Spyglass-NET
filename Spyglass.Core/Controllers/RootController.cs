using System.Net;
using Microsoft.AspNetCore.Mvc;
using Spyglass.Core.Database;
using Spyglass.Models;

namespace SpyglassNET.Controllers
{
    [ApiController]
    [Route("/")]
    public class RootController : Controller
    {
        private readonly IConfigurationRoot _config;
        private readonly SpyglassContext _context;

        public RootController(IConfigurationRoot config, SpyglassContext context)
        {
            _config = config;
            _context = context;
        }

        [HttpGet]
        public IActionResult Get()
        {
            return RedirectPermanent(_config["SpyglassDiscordInvite"]);
        }

        /// <summary>
        /// Returns data about the API's current version, and the minimum client version required to interact with it. 
        /// </summary>
        // TODO: Update models submodule, use ApiVersion
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("version")]
        public ActionResult<ApiVersion> GetVersion()
        {
            return Ok(new ApiVersion
            {
                Success = true,
                Version = _config["SpyglassVersion"],
                MinimumVersion = _config["SpyglassMinimumVersion"]
            });
        }

        /// <summary>
        /// Returns current statistics about tracked players and sanctions in the database.
        /// </summary>
        /// <returns> An ApiStats object containing stats on success, or an error message on failure. </returns>
        [HttpGet]
        [Route("stats")]
        public ActionResult<ApiStats> GetStats()
        {
            var playerCounts = _context.Players
                .Select(p => new
                {
                    SanctionCount = p.Sanctions.Count()
                })
                .ToList();

            return Ok(new ApiStats
            {
                Success = true,
                Players = playerCounts.Count,
                Sanctions = playerCounts.Sum(c => c.SanctionCount)
            });
        }

        [HttpGet]
        [Route("fallback")]
        public IActionResult GetFallback(int statusCode)
        {
            Response.StatusCode = statusCode;
            return Json(new ApiResult
            {
                Success = false,
                Error = $"{statusCode} {((HttpStatusCode) statusCode).ToString()}"
            });
        }
    }
}