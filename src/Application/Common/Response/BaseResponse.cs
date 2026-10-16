namespace Application.Common.Responce;

public class BaseResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    public static BaseResponse Ok(string? message = null)
        => new() { Success = true, Message = message };

    public static BaseResponse Fail(string message)
        => new() { Success = false, Message = message };
}
