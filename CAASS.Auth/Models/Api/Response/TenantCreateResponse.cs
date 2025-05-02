namespace CAASS.Auth.Models.Api.Response;

public class TenantCreateResponse : BaseResponse<TenantCreateResponseData>
{
    public TenantCreateResponse(TenantCreateResponseData data) : base(data)
    {
        Message = "Tenant created successfully!";
    }

    public TenantCreateResponse(string errorMessage) : base(errorMessage) { }
}

public record TenantCreateResponseData
{
    public Guid TenantId { get; init; }
}