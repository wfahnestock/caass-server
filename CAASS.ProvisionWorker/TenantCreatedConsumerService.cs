using System.Collections.Concurrent;
using System.Text;
using CAASS.ProvisionWorker.Messaging;
using CAASS.ProvisionWorker.Models.Context;
using CAASS.ProvisionWorker.Models.Events;
using CAASS.ProvisionWorker.Utils;
using Docker.DotNet;
using Docker.DotNet.Models;
using Microsoft.EntityFrameworkCore;
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
    private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(32_000);
    private readonly ConcurrentBag<DockerClient> _dockerClientPool = new();
    private readonly int _maxPoolSize;
    private int _currentPoolSize = 0;
    private IConnection _connection;
    private IChannel _channel;

    public TenantCreatedConsumerService(IOptions<RabbitMqSettings> rabbitMqSetting, IServiceProvider serviceProvider, ILogger<TenantCreatedConsumerService> logger, IConfiguration configuration)
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

        _maxPoolSize = configuration.GetValue<int>("DockerClientPoolSize", 10);

        InitializeDockerClientPool();
    }

    private void InitializeDockerClientPool()
    {
        _logger.LogInformation($"Initializing Docker client pool with {_maxPoolSize} clients");
        
        for (int i = 0; i < _maxPoolSize; i++)
        {
            _dockerClientPool.Add(CreateDockerClient());
            Interlocked.Increment(ref _currentPoolSize);
        }
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
        
        _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: true, cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            _logger.LogInformation($"Received message: {message}");

            bool isProcessed = false;

            await _semaphore.WaitAsync(cancellationToken);
            try
            {
                isProcessed = await ProcessMessage(message);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception occurred while processing message from queue {queueName}: {ex}");
            }
            finally
            {
                _semaphore.Release();
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
    
        DockerClient? client = null;
        string? containerId = null;
        bool success = false;
        string dbPassword = "";
    
        var context = JsonConvert.DeserializeObject<TenantCreatedEvent>(message);
        if (context == null)
        {
            _logger.LogError("Failed to deserialize message to TenantCreatedEvent");
            return false;
        }
    
        try
        {
            client = GetDockerClient();
            _logger.LogDebug("Retrieved Docker client from pool");
    
            string containerName = $"{context.TenantSlug}-db";
            string volumeName = $"{context.TenantSlug}-data";
            dbPassword = CryptoDeterministicStringGenerator.Generate(context.TenantSlug, 24);
            
            _logger.LogInformation($"Database password for database {context.TenantSlug} is {dbPassword}");
    
            // Check if container already exists
            var containers = await client.Containers.ListContainersAsync(
                new ContainersListParameters { All = true });
    
            var existingContainer = containers.FirstOrDefault(c =>
                c.Names.Contains($"/{containerName}"));
    
            if (existingContainer != null)
            {
                _logger.LogInformation($"Container {containerName} already exists with ID {existingContainer.ID}");
                containerId = existingContainer.ID;
                
                // Check if it's running
                if (existingContainer.State == "running")
                {
                    _logger.LogInformation($"Container {containerName} is already running, will proceed with migration");
                    success = true;
                }
                else
                {
                    // Container exists but not running, start it
                    _logger.LogInformation($"Container {containerName} exists but not running, starting it");
                    await client.Containers.StartContainerAsync(existingContainer.ID, new ContainerStartParameters());
                    _logger.LogInformation($"Started existing container {containerName}");
                    success = true;
                }
            }
            else
            {
                // Create volume if it doesn't exist
                try
                {
                    await client.Volumes.CreateAsync(new VolumesCreateParameters { Name = volumeName });
                }
                catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    _logger.LogInformation($"Volume {volumeName} already exists");
                }
    
                _logger.LogInformation($"Creating container for tenant {context.TenantSlug}");
                
                try
                {
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
                                {
                                    "5432/tcp", new List<PortBinding>
                                    {
                                        new PortBinding { HostPort = "" }
                                    }
                                }
                            },
                            PublishAllPorts = true,
                            Binds = new List<string> { $"D:/{volumeName}:/var/lib/postgresql/data" }
                        },
                        ExposedPorts = new Dictionary<string, EmptyStruct>
                        {
                            { "5432/tcp", new EmptyStruct() }
                        },
                    });
                    
                    containerId = response.ID;
                    await client.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
                    _logger.LogInformation($"Container {containerName} with ID {containerId} created and started");
                    success = true;
                }
                catch (DockerApiException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Conflict)
                {
                    // Container was created by another instance between our check and create
                    _logger.LogInformation($"Container {containerName} was created by another process, will reuse it");
                    
                    // Get the container ID that was just created
                    containers = await client.Containers.ListContainersAsync(
                        new ContainersListParameters { All = true });
                    
                    existingContainer = containers.FirstOrDefault(c => c.Names.Contains($"/{containerName}"));
                    if (existingContainer != null)
                    {
                        containerId = existingContainer.ID;
                        
                        // Make sure it's running
                        if (existingContainer.State != "running")
                        {
                            await client.Containers.StartContainerAsync(containerId, new ContainerStartParameters());
                        }
                        
                        success = true;
                    }
                }
            }
    
            if (success && !string.IsNullOrEmpty(containerId))
            {
                // Get the port for the running container with retry
                string? hostPort = await GetContainerPortWithRetry(client, containerId);
                
                if (!string.IsNullOrEmpty(hostPort))
                {
                    _logger.LogInformation($"Container running on port: {hostPort}");
                    
                    if (await ApplyMigrations(context.TenantSlug, hostPort, dbPassword))
                    {
                        _logger.LogInformation($"Successfully applied migrations for tenant {context.TenantSlug}");
                        return true;
                    }
                    else
                    {
                        _logger.LogError($"Failed to apply migrations for tenant {context.TenantSlug}");
                    }
                }
            }
            
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing tenant {context.TenantSlug}");
            return false;
        }
        finally
        {
            if (client != null)
            {
                ReturnDockerClient(client);
                _logger.LogDebug("Returned Docker client to pool");
            }
        }
    }
    
    private async Task<string?> GetContainerPortWithRetry(DockerClient client, string containerId, int maxRetries = 5)
    {
        _logger.LogInformation($"Checking ContainerID: {containerId} for port information");
        
        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                var containerInfo = await client.Containers.InspectContainerAsync(containerId);
            
                if (containerInfo?.NetworkSettings?.Ports != null && 
                    containerInfo.NetworkSettings.Ports.TryGetValue("5432/tcp", out var portBindings) &&
                    portBindings is { Count: > 0 })
                {
                    return portBindings[0].HostPort;
                }
            
                _logger.LogWarning($"Port information not available on attempt {attempt+1}, retrying...");
                await Task.Delay(1000);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to get port on attempt {attempt+1}, retrying...");
                await Task.Delay(1000);
            }
        }
    
        return null;
    }

    private async Task<bool> ApplyMigrations(string slug, string port, string password)
    {
        const int maxRetries = 10;
        const int retryDelayMs = 3000;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            try
            {
                _logger.LogInformation($"Attempt {attempt + 1} to apply migrations on database {slug}");
                
                var connectionString = $"Host=host.docker.internal;Port={port};Database={slug}-db;Username=caass;Password={password}";
                
                await using var context = new TenantDbContext(connectionString);

                await context.Database.CanConnectAsync();

                var migrations = await context.Database.GetPendingMigrationsAsync();

                if (migrations.Any())
                {
                    await context.Database.MigrateAsync();
                }
                
                _logger.LogInformation($"Successfully applied migrations on database {slug} on attempt {attempt + 1}");
                return true;
            } 
            catch (Exception ex)
            {
                _logger.LogError($"Attempt {attempt + 1} failed with message: {ex.Message}");
                await Task.Delay(retryDelayMs);
            }
        }

        return false;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        // Dispose all Docker clients in the pool
        while (_dockerClientPool.TryTake(out var client))
        {
            client.Dispose();
        }

        await base.StopAsync(cancellationToken);
    }

    private DockerClient CreateDockerClient()
    {
        if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
        {
            return new DockerClientConfiguration(new Uri("npipe://./pipe/docker_engine")).CreateClient();
        }
        else
        {
            return new DockerClientConfiguration(new Uri("unix:///var/run/docker.sock")).CreateClient();
        }
    }

    private DockerClient GetDockerClient()
    {
        if (_dockerClientPool.TryTake(out var client))
        {
            Interlocked.Decrement(ref _currentPoolSize);
            return client;
        }
        
        _logger.LogInformation("Docker client pool empty, creating new client");
        return CreateDockerClient();
    }
    
    private void ReturnDockerClient(DockerClient client)
    {
        if (_currentPoolSize < _maxPoolSize)
        {
            _dockerClientPool.Add(client);
            Interlocked.Increment(ref _currentPoolSize);
        }
        else
        {
            client.Dispose();
        }
    }
}