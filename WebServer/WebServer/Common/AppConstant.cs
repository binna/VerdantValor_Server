namespace WebServer.Contexts
{
    public static partial class AppConstant
    {
        public static class Route
        {
            public const string API_BASE = "api/[controller]";
        }

        public static class Jwt
        {
            // TODO 비밀키 환경설정으로 이동
            public const string ISSUER = "DemoAuthServer";
            public const string AUDIENCE = "DemoClient";
            public const string SECRET_KEY = "ThisIsASecretKeyForJwtTokenDemoT";
            public const int EXPIRE_MINUTES = 30;
        }
    }
}
