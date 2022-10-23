using Newtonsoft.Json;

namespace Spyglass.Models
{
    /// <summary>
    /// Basic information about a tracked Titanfall 2 player.
    /// </summary>
    public class PlayerInfo
    {
        /// <summary>
        /// The unique id (UID) of the player.
        /// </summary>
        public string UniqueID { get; set; } = default!;

        /// <summary>
        /// The username this player was initially seen with.
        /// </summary>
        public string Username { get; set; } = default!;

        /// <summary>
        /// Whether or not this player is a maintainer for Spyglass.
        /// </summary>
        public bool IsMaintainer { get; set; } = false;
        
        /// <summary>
        /// The date and time at which this player was added to the Spyglass database.
        /// </summary>
        public DateTimeOffset CreatedAt { get; private set; }
        
        /// <summary>
        /// List of known username aliases for this player.
        /// </summary>
        [JsonIgnore]
        public List<PlayerAlias> Aliases { get; private set; }

        /// <summary>
        /// List of known username aliases for this player, in string form only.
        /// </summary>
        public List<string>? KnownAliases => Aliases?.Select(a => a.Alias).ToList() ?? null;
        
        /// <summary>
        /// List of known sanctions for this player.
        /// </summary>
        public List<PlayerSanction> Sanctions { get; private set; }
    }
}

