namespace Spyglass.Models
{
    /// <summary>
    /// A username alias for a given player.
    /// </summary>
    public class PlayerAlias
    {
        /// <summary>
        /// The unique id (UID) of the player for whom this username alias belongs to.
        /// </summary>
        public string UniqueID { get; set; }
        
        /// <summary>
        /// The additional username this player has over their primary one.
        /// </summary>
        public string Alias { get; set; }

        /// <summary>
        /// The player that this alias belongs to.
        /// </summary>
        public PlayerInfo OwningPlayer { get; set; }
    }   
}