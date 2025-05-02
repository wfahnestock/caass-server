namespace CAASS.Auth.Messaging;

public class RabbitMqSettings
{
    public string Host { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public static class RabbitMqQueues
    {
        public const string TenantCreatedQueue = "tenant-created-queue";
        
        // Add more above as needed...
    }
}