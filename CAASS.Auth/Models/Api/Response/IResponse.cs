namespace CAASS.Auth.Models.Api.Response;

public interface IResponse<T>
{
    public bool IsError { get; }
    public string Message { get; }
    public T? Data { get; }
}