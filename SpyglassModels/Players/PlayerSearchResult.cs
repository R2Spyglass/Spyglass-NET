namespace Spyglass.Models
{
    /// <summary>
    /// Contains the result of an attempt to search for a player, either by name or uid.
    /// </summary>
    public class PlayerSearchResult : ApiResult
    {
        /// <summary>
        /// The unique id (UID) that was used to search for the user, if any.
        /// </summary>
        public string? UniqueID { get; set; }
        
        /// <summary>
        /// The username that was used to search for the user, if any.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// The list of players who match the query, if any.
        /// </summary>
        public List<PlayerInfo> Matches { get; set; } = new();
    }
}

