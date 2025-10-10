namespace FatSecret.Domain.Entities.Response;

public class ApiResponse
{
    public object Result { get; set; }
    public int StatusCode { get; set; } = 200;
    public DateTimeOffset Date { get; set; } = DateTimeOffset.Now;

    public ApiResponse() { }

    public ApiResponse(int statusCode)
    {
        StatusCode = statusCode;
    }
}

public class ApiResponse<T>
{
    public T Result { get; set; }
    public int StatusCode { get; set; } = 200;
    public DateTimeOffset Date { get; set; } = DateTimeOffset.Now;

    public ApiResponse() { }

    public ApiResponse(T data)
    {
        Result = data;
    }

    public ApiResponse(T data, int statusCode)
    {
        Result = data;
        StatusCode = statusCode;
    }
}