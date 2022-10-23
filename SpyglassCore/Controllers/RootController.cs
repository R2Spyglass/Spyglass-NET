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

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [Route("version")]
        public IActionResult GetVersion()
        {
            return Ok(new
            {
                Version = _config["SpyglassVersion"],
                MinimumVersion = _config["SpyglassMinimumVersion"]
            });
        }

        [HttpGet]
        [Route("stats")]
        public IActionResult GetStats()
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