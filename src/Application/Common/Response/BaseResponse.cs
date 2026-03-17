namespace Application.Common.Responce;

public class BaseResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }   // 👈 əlavə et

    public static BaseResponse<T> Ok(T data, string? message = null)
        => new() { Success = true, Data = data, Message = message };

    public static BaseResponse<T> Fail(string message)
        => new() { Success = false, Message = message };

    public static BaseResponse<T> Fail(string message, List<string> errors) // 👈 overload
        => new() { Success = false, Message = message, Errors = errors };
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
