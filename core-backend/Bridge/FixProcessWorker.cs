using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;
using System.Text;
using FixBackendShared.Grpc;
using Grpc.Core;

namespace Bridge;

public class FixProcessWorker(ILogger<FixProcessWorker> logger, FixProcessing.FixProcessingClient fixProcessingClient) : BackgroundService
{
	private const string ProcessRequestQueueName = "fix_messages_process";
	private const string ProcessConfirmationQueueName = "fix_messages_confirmation";

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	private readonly ILogger<FixProcessWorker> _logger = logger;
	private readonly FixProcessing.FixProcessingClient fixProcessingClient = fixProcessingClient;
	private CancellationToken _cancellationToken;

	private IConnection? _connection;
	private IChannel? _channel;

	protected override async Task ExecuteAsync(CancellationToken cancellationToken)
	{
		_cancellationToken = cancellationToken;

		var uri = Environment.GetEnvironmentVariable("RABBITMQ_URI")
			?? throw new InvalidOperationException("RABBITMQ_URI is not set");

		var factory = new ConnectionFactory { Uri = new Uri(uri) };

		_connection = await factory.CreateConnectionAsync(cancellationToken);
		_channel = await _connection.CreateChannelAsync(
			new CreateChannelOptions(
				publisherConfirmationsEnabled: true, // We want to make sure BasicPublishAsync message is received by the broker, if not throw from BasicPublishAsync, NACK and retry
				publisherConfirmationTrackingEnabled: true
			),
			cancellationToken: cancellationToken);

		await _channel.QueueDeclareAsync(
			queue: ProcessRequestQueueName,
			durable: true,
			exclusive: false,
			autoDelete: false,
			arguments: null,
			cancellationToken: cancellationToken
		);

		await _channel.QueueDeclareAsync(
			queue: ProcessConfirmationQueueName,
			durable: true,
			exclusive: false,
			autoDelete: false,
			arguments: null,
			cancellationToken: cancellationToken
		);

		await _channel.BasicQosAsync(0, prefetchCount: 1, global: false, cancellationToken);

		var consumer = new AsyncEventingBasicConsumer(_channel);
		consumer.ReceivedAsync += OnReceivedAsync;

		await _channel.BasicConsumeAsync(ProcessRequestQueueName, autoAck: false, consumer, cancellationToken);

		_logger.LogInformation("Waiting for fix persisted events on {Queue}", ProcessRequestQueueName);
	}

	private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs args)
	{
		try
		{
			var evt = JsonSerializer.Deserialize<FixProcessRequest>(args.Body.Span, JsonOptions);

			if (evt is null)
			{
				_logger.LogWarning("Bad message, dropping");
				await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
				return;
			}

			using var _ = _logger.BeginScope(new Dictionary<string, object> { ["fixId"] = evt.Id } );

			var resp = await fixProcessingClient.ProcessAsync(evt, cancellationToken: _cancellationToken);

			await _channel!.BasicPublishAsync(
				exchange: string.Empty,
				routingKey: ProcessConfirmationQueueName,
				mandatory: true, // We want to make sure messages from BasicPublishAsync are routable when sending, if not throw from BasicPublishAsync, NACK and try again
				basicProperties: new BasicProperties { Persistent = true },
				body: Encoding.UTF8.GetBytes(evt.Id),
				cancellationToken: _cancellationToken);

			_logger.LogInformation("Fix {Id} processed", evt.Id);

			await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false);
		}
		catch (JsonException je)
		{
			// In case of malformed json which might cause a poison loop
			_logger.LogError(je, "Malformed message, dropping");
			await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
		}
		catch (RpcException rpc) when (rpc.StatusCode is StatusCode.NotFound or StatusCode.InvalidArgument)
		{
			_logger.LogError(rpc, "Server rejected as unprocessable, dropping");
			await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
		}
		catch (Exception e)
		{
			_logger.LogError(e, "Transient failure, requeueing");
			await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: true);
		}
	}

	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		if (_channel is not null)
			await _channel.CloseAsync(cancellationToken);
		if (_connection is not null)
			await _connection.CloseAsync(cancellationToken);
		
		await base.StopAsync(cancellationToken);
	}
}
