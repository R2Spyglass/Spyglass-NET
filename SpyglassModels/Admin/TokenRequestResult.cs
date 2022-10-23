namespace Spyglass.Models.Admin
{
    /// <summary>
    /// Result of a token request from the admin controller.
    /// </summary>
    public class TokenRequestResult : ApiResult
    {
        /// <summary>
        /// The token that was returned by the identity server.
        /// </summary>
        public string Token { get; init; }

        /// <summary>
        /// The type of token that was returned by the identity server.
        /// </summary>
        public string TokenType { get; init; }

        /// <summary>
        /// The scope returned by the identity server.
        /// </summary>
        public string Scope { get; init; }
    }
}

