using System.Text;
using System.Text.Json;
using CAASS.Auth.Messaging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace CAASS.Auth.Services.Implementations;

public class RabbitMqPublisher<T> : IRabbitMqPublisher<T>
{
    private readonly RabbitMqSettings _rabbitMqSettings;

    public RabbitMqPublisher(IOptions<RabbitMqSettings> rabbitMqSettings)
    {
        _rabbitMqSettings = rabbitMqSettings.Value;
    }

    public Task PublishAsync(T message, string queueName)
    {
        var factory = new ConnectionFactory
        {
            HostName = _rabbitMqSettings.Host,
            UserName = _rabbitMqSettings.Username,
            Password = _rabbitMqSettings.Password,
        };

        using var connection = factory.CreateConnectionAsync(CancellationToken.None);
        using var channel = connection.Result.CreateChannelAsync();

        channel.Result.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        var props = new BasicProperties();

        //channel.Result.BasicPublishAsync(exchange: "", routingKey: queueName, basicProperties: null, body: body);
        channel.Result.BasicPublishAsync(exchange: "", routingKey: queueName, mandatory: true, basicProperties: props, body: body);

        return Task.CompletedTask;
    }
}