// ReSharper disable UnusedAutoPropertyAccessor.Local
// ReSharper disable UnusedAutoPropertyAccessor.Global

using Newtonsoft.Json;

namespace Spyglass.Models
{
    /**
     * Details about a sanction that was issued to a player.
     */
    public class PlayerSanction
    {
        /// <summary>
        /// The unique id given to this player sanction.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The unique id (UID) of the player this sanction belongs to.
        /// </summary>
        public string UniqueId { get; set; } = default!;

        /// <summary>
        /// The player this sanction belongs to.
        /// </summary>
        public PlayerInfo OwningPlayer { get; set; } = null!;

        /// <summary>
        /// The Discord id of the player who issued this sanction.
        /// </summary>
        public ulong IssuerId { get; set; } = default!;

        /// <summary>
        /// The time at which this sanction was issued.
        /// </summary>
        [JsonIgnore]
        public DateTimeOffset IssuedAt { get; set; }

        /// <summary>
        /// The unix timestamp seconds of the date & time the sanction was issued.
        /// </summary>
        [JsonProperty("issuedAt")]
        public long IssuedAtTimestamp => IssuedAt.ToUnixTimeSeconds();

        /// <summary>
        /// The time at which this sanction was issued, in a readable string format.
        /// </summary>
        public string IssuedAtReadable => IssuedAt.ToString("R");
        
        /// <summary>
        /// The time at which this sanction expires, if any.
        /// Null or otherwise empty values mean this sanction is permanent.
        /// </summary>
        [JsonIgnore]
        public DateTimeOffset? ExpiresAt { get; set; }
        
        /// <summary>
        /// The unix timestamp seconds of the date & time this sanction expires, if any.
        /// Null or otherwise empty values mean this sanction is permanent.
        /// </summary>
        [JsonProperty("expiresAt", NullValueHandling = NullValueHandling.Include)]
        public long? ExpiresAtTimestamp => ExpiresAt?.ToUnixTimeSeconds() ?? null;

        /// <summary>
        /// The time at which this sanction was issued, in a readable string format.
        /// </summary>
        public string ExpiresAtReadable => ExpiresAt?.ToString("R") ?? "Never";

        /// <summary>
        /// The reason why this sanction was applied to the player.
        /// </summary>
        public string Reason { get; set; } = default!;
        
        /// <summary>
        /// A link to the report message on the Northstar community Discord server.
        /// </summary>
        public string ReportMessage { get; set; } = default!;

        /// <summary>
        /// The type of infraction that led to this sanction.
        /// </summary>
        public InfractionType Type { get; set; }

        /// <summary>
        /// A human readable, uppercase string of the sanction's infraction type.
        /// Can be used for filtering on the game mod side.
        /// </summary>
        public string TypeString => Enum.GetName(Type)!.ToUpper();

        /// <summary>
        /// The punishment that should be applied for this sanction.
        /// </summary>
        public SanctionType PunishmentType { get; set; }

        /// <summary>
        /// A human readable, uppercase string of the sanction's punishment type.
        /// </summary>
        public string PunishmentString => Enum.GetName(PunishmentType)!.ToUpper();
    }
}