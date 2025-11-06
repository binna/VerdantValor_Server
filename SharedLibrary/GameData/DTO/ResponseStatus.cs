namespace SharedLibrary.GameData.DTO;

public class ResponseStateDto
{
    public List<ResponseStateItemDto> Data { get; set; } = [];
}

public class ResponseStateItemDto
{
    public int Code { get; set; }
    public bool IsSuccess { get; set; }
    public required List<string> Message { get; set; }
}