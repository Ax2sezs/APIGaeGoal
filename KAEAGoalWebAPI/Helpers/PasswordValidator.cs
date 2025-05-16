using System.Text.RegularExpressions;

namespace KAEAGoalWebAPI.Helpers
{
    public static class PasswordValidator
    {
        public static bool IsValidPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return false;

            var hasUpperCase = Regex.IsMatch(password, "[A-Z]");
            var hasLowerCase = Regex.IsMatch(password, "[a-z]");
            var hasDigit = Regex.IsMatch(password, @"\d");
            var hasSpecialChar = Regex.IsMatch(password, @"[\W_]");

            return hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar;
        }
    }
}
