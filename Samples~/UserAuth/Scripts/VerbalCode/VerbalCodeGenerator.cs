namespace Cognitive3D.Auth
{
    /// <summary>
    /// Generates and formats random numeric codes for the Verbal Code identification method.
    /// </summary>
    public static class VerbalCodeGenerator
    {
        /// <summary>
        /// Generates a random numeric string of the specified length
        /// </summary>
        public static string GenerateCode(int length = 6)
        {
            char[] digits = new char[length];
            for (int i = 0; i < length; i++)
            {
                digits[i] = (char)('0' + UnityEngine.Random.Range(0, 10));
            }
            return new string(digits);
        }

        /// <summary>
        /// Formats a code string for display by inserting a dash at the midpoint
        /// "472815" becomes "472-815"
        /// </summary>
        public static string FormatForDisplay(string code)
        {
            if (string.IsNullOrEmpty(code) || code.Length < 2)
                return code;

            int mid = code.Length / 2;
            return code.Substring(0, mid) + "-" + code.Substring(mid);
        }
    }
}
