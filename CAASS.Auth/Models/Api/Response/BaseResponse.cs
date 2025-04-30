namespace CAASS.Auth.Models.Api.Response;

public class BaseResponse<T> : IResponse<T>
{
    public bool IsError { get; set; } = false;
    public string Message { get; set; } = "";
    public T? Data { get; set; }

    public BaseResponse() { }

    protected BaseResponse(T data)
    {
        Data = data;
    }
    
    protected BaseResponse(string errorMessage)
    {
        IsError = true;
        Message = errorMessage;
    }
    
}