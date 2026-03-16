namespace Application.Common.Responce;

public class BaseResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }

    public static BaseResponse<T> Ok(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    public static BaseResponse<T> Fail(string message)
        => new() { Success = false, Message = message };
}
public class BaseResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }

    public static BaseResponse Ok(string? message = null)
        => new() { Success = true, Message = message };

    public static BaseResponse Fail(string message)
        => new() { Success = false, Message = message };
}
