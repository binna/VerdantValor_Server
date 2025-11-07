namespace SharedLibrary.Common;

public static class AppConstant
{
    public const string WEB_SERVER_API_BASE = "api";

    public const int EAMIL_MIN_LENGTH = 5;
    public const int EAMIL_MAX_LENGTH = 50;

    public const int NICKNAME_MIN_LENGTH = 3;
    public const int NICKNAME_MAX_LENGTH = 30;

    public const int RANKING_MIN = 50;
    public const int RANKING_MAX = 100;

    public const string RANKING_ROOT = "Ranking";
    
    public const string SHARED_LIBRARY_PATH = @"..\..\..\..\SharedLibrary";

    public enum ERankingScope
    {
        My = 1,
        Global = 2
    }

    public enum ERankingType
    {
        All = 1
    }

    public enum EAuthType
    {
        Login = 1,
        Join = 2
    }

    public enum ELanguage
    {
        Ko = 0,
        En = 1,
    }
}