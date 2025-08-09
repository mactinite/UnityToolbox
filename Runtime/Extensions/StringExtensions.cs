namespace toolbox.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Computes a non-cryptographic hash code for the string using the FNV-1a algorithm.
        /// </summary>
        /// <param name="str">input string</param>
        /// <returns>An integer representing the FNV-1a hash of the input string</returns>
        public static int ComputeFNV1aHash(this string str)
        {
            uint hash = 2166136261;
            foreach (char c in str)
            {
                hash = (hash ^ c) * 16777619;
            }

            return unchecked((int)hash);
        }
    }
}