namespace Spyglass.Models
{
    /// <summary>
    /// The type of infraction that resulted in a player sanction.
    /// </summary>
    public enum InfractionType
    {
        /// <summary>
        /// Spamming text and/or voice chat. 
        /// </summary>
        Spamming = 0,
        /// <summary>
        /// Harassing a player through text and/or voice chat.
        /// </summary>
        Harassment = 1,
        /// <summary>
        /// Any discriminatory, threatening, abusive, hateful or otherwise offensive language.
        /// This is a level higher than harassment.
        /// </summary>
        ForbiddenLanguage = 2,
        /// <summary>
        /// Attempts to disrupt the game experience of one or more players outside of the accepted gameplay parameters.
        /// A good example would be purposefully causing damage to a teammate, either directly or indirectly. 
        /// </summary>
        Griefing = 3,
        /// <summary>
        /// Use or attempted use of exploits that may deteriorate the performance of game clients and/or the server, or otherwise maliciously impact the set gameplay parameters of Titanfall 2.
        /// </summary>
        Exploiting = 4,
        /// <summary>
        /// The use of third-party hacking or cheating clients or any tool that provides an advantage, like macros.
        /// </summary>
        Cheating = 5,
    }
}
