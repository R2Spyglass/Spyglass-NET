using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Spyglass.Core.Services;
using Spyglass.Models;
using Spyglass.Models.Admin;

namespace SpyglassNET.Controllers
{
    [ApiController]
    [RequireHttps]
    [Route("authenticate/")]
    public class AuthenticationController : Controller
    {
        private readonly MaintainerAuthenticationService _maintainerAuth;

        public AuthenticationController(MaintainerAuthenticationService maintainerAuth)
        {
            _maintainerAuth = maintainerAuth;
        }
        
        [HttpGet]
        [Authorize("admin")]
        [Route("request")]
        public ActionResult<MaintainerAuthenticationResult> RequestMaintainerTicket(string uniqueId)
        {
            if (!User.HasClaim(c => c.Type == "client_id"))
            {
                return new MaintainerAuthenticationResult
                {
                    Success = false,
                    Error = "Cannot authenticate maintainer: no client_id claim."
                };
            }

            return Ok(_maintainerAuth.CreateAuthenticationTicket(User.FindFirstValue("client_id"), uniqueId));
        }

        [HttpGet]
        [Route("validate")]
        public ActionResult<MaintainerTicketValidationResult> ValidateMaintainerTicket(string uniqueId, string token)
        {
            return Ok(_maintainerAuth.ValidateAuthenticationTicket(uniqueId, token));
        }
    }
}

