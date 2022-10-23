namespace Spyglass.Models
{
    /// <summary>
    /// Contains the result of an attempt to search for a sanction, either by id or owning player uid.
    /// </summary>
    public class SanctionSearchResult : ApiResult
    {
        /// <summary>
        /// The unique id (UID) that was used to search for the sanctions, if any.
        /// </summary>
        public List<string>? UniqueIDs { get; set; }
        
        /// <summary>
        /// The id that was used to search for the sanction, if any.
        /// </summary>
        public int? Id { get; set; }

        /// <summary>
        /// The list of players who match the query, if any.
        /// </summary>
        public Dictionary<string, List<PlayerSanction>>? Matches { get; set; }
    }
}

