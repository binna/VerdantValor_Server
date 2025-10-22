namespace WebServer.Contexts
{
    public static partial class AppConstant
    {
        public static class Route
        {
            public const string API_BASE = "api/[controller]";
        }

        public const byte emailMinLength = 5;
        public const byte emailMaxLength = 50;

        public const byte nicknameMinLength = 3;
        public const byte nicknameMaxLength = 30;
    }
}