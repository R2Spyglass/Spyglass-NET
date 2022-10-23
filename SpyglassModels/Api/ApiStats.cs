namespace Spyglass.Models
{
    /// <summary>
    /// Stats returned by the /stats endpoint.
    /// </summary>
    public class ApiStats : ApiResult
    {
        /// <summary>
        /// The amount of players tracked in the database.
        /// </summary>
        public int Players { get; set; }
        
        /// <summary>
        /// The amount of sanctions tracked in the database.
        /// </summary>
        public int Sanctions { get; set; }
    } 
}

