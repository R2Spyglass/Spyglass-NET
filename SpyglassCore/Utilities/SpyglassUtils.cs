namespace SpyglassNET.Utilities
{
    public static class SpyglassUtils
    {
        /// <summary>
        /// Checks if the given unique id (UID) is valid.
        /// </summary>
        /// <param name="uniqueId">The unique id to validate based on known possible unique ids.</param>
        /// <returns> True if the unique id is formatted correctly, false if not. </returns>
        public static bool ValidateUniqueId(string uniqueId)
        {
            if (string.IsNullOrWhiteSpace(uniqueId))
            {
                return false;
            }

            if (uniqueId.Length > 20)
            {
                return false;
            }

            if (!ulong.TryParse(uniqueId, out ulong parsed))
            {
                return false;
            }

            return true;
        }
    }
}

