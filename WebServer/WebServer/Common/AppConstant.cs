namespace WebServer.Contexts
{
    public static class AppConstant
    {
        public static class Route
        {
            public const string API_BASE = "api/[controller]";
        }

        public const int EAMIL_MinLength = 5;
        public const int EAMIL_MAX_LENGTH = 50;

        public const int NICKNAME_MIN_LENGTH = 3;
        public const int NICKNAME_MAX_LENGTH = 30;

        public const int RANKING_MIN = 50;
        public const int RANKING_MAX = 100;

        public const string RANKING_ROOT = "Ranking";

        public enum RankingType
        {
            All = 1
        }
    }
}