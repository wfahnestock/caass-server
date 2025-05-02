using System.Text;
using CAASS.ProvisionWorker.Messaging;
using CAASS.ProvisionWorker.Models.Events;
using CAASS.ProvisionWorker.Utils;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace CAASS.ProvisionWorker;

public class TenantCreatedConsumerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<TenantCreatedConsumerService> _logger;
    private readonly RabbitMqSettings _rabbitMqSetting;
    private IConnection _connection;
    private IChannel _channel;

    public TenantCreatedConsumerService(IOptions<RabbitMqSettings> rabbitMqSetting, IServiceProvider serviceProvider, ILogger<TenantCreatedConsumerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _rabbitMqSetting = rabbitMqSetting.Value;
        
        var factory = new ConnectionFactory
        {
            HostName = _rabbitMqSetting.Host,
            UserName = _rabbitMqSetting.Username,
            Password = _rabbitMqSetting.Password
        };
        _connection = factory.CreateConnectionAsync().Result;
        _channel = _connection.CreateChannelAsync().Result;
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        StartConsuming(RabbitMqSettings.RabbitMqQueues.TenantCreatedQueue, stoppingToken);
        await Task.CompletedTask;
    }

    private void StartConsuming(string queueName, CancellationToken cancellationToken)
    {
        _channel.QueueDeclareAsync(queue: queueName, durable: true, exclusive: false, autoDelete: false, arguments: null, cancellationToken: cancellationToken)
            .Wait(cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation($"Received message: {message}");
            
            bool isProcessed = await ProcessMessage(message);

            try
            {
                isProcessed = await ProcessMessage(message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurred while processing message from queue {queueName}: {ex}");
            }

            if (isProcessed)
            {
                // Acknowledge the message
                await _channel.BasicAckAsync(deliveryTag: ea.DeliveryTag, multiple: false, cancellationToken: cancellationToken);
            }
            else
            {
                // Reject the message and requeue it
                await _channel.BasicRejectAsync(deliveryTag: ea.DeliveryTag, requeue: true, cancellationToken: cancellationToken);
            }
        };
        
        _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer, cancellationToken: cancellationToken);
    }
    
    private async Task<bool> ProcessMessage(string message)
    {
        _logger.LogInformation($"Processing message: {message}");
        
        var context = JsonConvert.DeserializeObject<TenantCreatedEvent>(message);
        
        DockerClient client = new DockerClientConfiguration()
            .CreateClient();

        string containerName = $"{context.TenantSlug}-db";
        string dbPassword = CryptoDeterministicStringGenerator.Generate(context.TenantSlug, 24);
        var response = await client.Containers.CreateContainerAsync(new CreateContainerParameters
        {
            Image = "postgres:latest",
            Name = containerName,
            Env = new List<string>
            {
                $"POSTGRES_USER=caass",
                $"POSTGRES_PASSWORD={dbPassword}",
                $"POSTGRES_DB={context.TenantSlug}-db"
            },
            HostConfig = new HostConfig
            {
                PortBindings = new Dictionary<string, IList<PortBinding>>
                {
                    { "5432/tcp", new List<PortBinding>
                        {
                            new PortBinding { HostPort = ""  } // dynamic port
                        } 
                    }
                }
            }
        });
        
        if (response.Warnings != null)
        {
            foreach (var warning in response.Warnings)
            {
                _logger.LogWarning($"Warning when creating container: {warning}");
            }
        }

        return await client.Containers.StartContainerAsync(containerName, new ContainerStartParameters());
    }
}