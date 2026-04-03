namespace WebServer.options;

// AppContext.BaseDirectory
//  실행 중인 애플리케이션의 주 실행 파일(host executable)이 위치한 디렉터리 경로를 반환

public class PathOption
{
    public string BaseDir { get; } = AppContext.BaseDirectory;
    public string SharedLibrary { get; set; }
    
    public string GameData =>
        Path.GetFullPath(
            Path.Combine(BaseDir, SharedLibrary, "GameData", "Data"));
}