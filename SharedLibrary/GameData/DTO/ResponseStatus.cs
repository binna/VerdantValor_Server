namespace SharedLibrary.GameData.DTO;

public class ResponseStatusDto
{
    public List<ResponseStatusItemDto> Data { get; set; } = [];
}

public class ResponseStatusItemDto
{
    public int Code { get; set; }
    public bool IsSuccess { get; set; }
    public required List<string> Message { get; set; }
}