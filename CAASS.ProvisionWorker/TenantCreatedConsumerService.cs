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

            bool isProcessed = false;

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
        if (context == null)
        {
            _logger.LogError("Failed to deserialize message to TenantCreatedEvent");
            return false;
        }
    
        try
        {
            // Configure Docker client
            DockerClient client;
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                client = new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
                _logger.LogInformation("Using Windows Docker named pipe");
            }
            else
            {
                client = new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
                _logger.LogInformation("Using Unix Docker socket");
            }
    
            string containerName = $"{context.TenantSlug}-db";
            string volumeName = $"{context.TenantSlug}-data";
            string dbPassword = CryptoDeterministicStringGenerator.Generate(context.TenantSlug, 24);
    
            // Check if container already exists
            var containers = await client.Containers.ListContainersAsync(
                new ContainersListParameters { All = true });
            
            var existingContainer = containers.FirstOrDefault(c => 
                c.Names.Contains($"/{containerName}"));
                
            if (existingContainer != null)
            {
                // Container exists
                _logger.LogInformation($"Container {containerName} already exists with ID {existingContainer.ID}");
                
                // Check if it's running
                if (existingContainer.State == "running")
                {
                    _logger.LogInformation($"Container {containerName} is already running, skipping creation");
                    return true;
                }
                else
                {
                    // Container exists but not running, remove it
                    _logger.LogInformation($"Container {containerName} exists but not running, removing it");
                    await client.Containers.RemoveContainerAsync(existingContainer.ID, new ContainerRemoveParameters { Force = true });
                    _logger.LogInformation($"Removed container {containerName}");
                }
            }
    
            // Create volume if it doesn't exist
            try
            {
                await client.Volumes.CreateAsync(new VolumesCreateParameters { Name = volumeName });
            }
            catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                _logger.LogInformation($"Volume {volumeName} already exists");
            }
            
            _logger.LogInformation($"Password: {dbPassword}");
    
            var response = await client.Containers.CreateContainerAsync(new CreateContainerParameters
            {
                Image = "postgres:16",
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
                                new PortBinding { HostPort = ""  }
                            }
                        }
                    },
                    PublishAllPorts = true,
                    Binds = new List<string> { $"{volumeName}:/var/lib/postgresql/data" }
                },
                ExposedPorts = new Dictionary<string, EmptyStruct>
                {
                    { "5432/tcp", new EmptyStruct() }
                },
            });
    
            bool started = await client.Containers.StartContainerAsync(response.ID, new ContainerStartParameters());
            _logger.LogInformation($"Container {containerName} started: {started}");
    
            return started;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error creating container for tenant {context.TenantSlug}");
            return false;
        }
    }
}