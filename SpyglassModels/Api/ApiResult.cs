namespace Spyglass.Models
{
    /// <summary>
    /// Returned by the API if an error occurs.
    /// </summary>
    public class ApiResult
    {
        /// <summary>
        /// Whether or not the API request was successful.
        /// </summary>
        public bool Success { get; init; }
        
        /// <summary>
        /// The message that was returned by the API for the error.
        /// </summary>
        public string? Error { get; init; } = null!;
    }
}
