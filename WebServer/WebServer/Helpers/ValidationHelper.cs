using System.Text.RegularExpressions;

namespace WebServer.Helpers
{
    public static class ValidationHelper
    {
        const string emailPattern = "^[a-zA-Z0-9]+$";

        public static bool IsValidEmail(string email)
        {
            return Regex.IsMatch(email, emailPattern);
        }
    }
}
