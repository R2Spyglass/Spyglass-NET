namespace Spyglass.Models
{
    /// <summary>
    /// The type of punishment that should be applied for a sanction.
    /// </summary>
    public enum SanctionType
    {
        /// <summary>
        /// The player should receive a visible warning, either in text chat or as a pop-up notification.
        /// </summary>
        Warn = 0,
        /// <summary>
        /// The player should be prevented from communicating using text and voice chat until the sanction expires.
        /// </summary>
        Mute = 1,
        /// <summary>
        /// The player will be banned from playing on Spyglass protected servers until the sanction expires.
        /// </summary>
        Ban = 2,
    }
}