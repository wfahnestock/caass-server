namespace CAASS.Auth.Models.Api.Response;

public class TenantAuthResponse : BaseResponse<TenantAuthResponseData>
{
    public TenantAuthResponse(TenantAuthResponseData data) : base(data) { }
    
    public TenantAuthResponse(string errorMessage) : base(errorMessage) { }
}

public record TenantAuthResponseData
{
    public Guid TenantId { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
}