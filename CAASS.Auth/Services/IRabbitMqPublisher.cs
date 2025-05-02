namespace CAASS.Auth.Services;

public interface IRabbitMqPublisher<T>
{
    Task PublishAsync(T message, string queueName);
}