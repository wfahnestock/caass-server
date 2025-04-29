namespace CAASS.Core.Models.Schema;

public record User
{
    public int UserId { get; set; }
    public required string UserName { get; set; }
    public required string Password { get; set; }
    public required string Salt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLogin { get; set; }
    public bool IsActive { get; set; }
    public bool IsLocked { get; set; }
}