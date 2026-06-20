using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text.Json;

namespace Orchestrator;

public record FixPersistedMessage(string Id, string Message);

public class FixProcessWorker(ILogger<FixProcessWorker> logger) : BackgroundService
{
	private const string QueueName = "fix_messages_process";

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase
	};

	private readonly ILogger<FixProcessWorker> _logger = logger;
	private IConnection? _connection;
	private IChannel? _channel;

	protected override async Task ExecuteAsync(CancellationToken cancellationToken)
	{
		var uri = Environment.GetEnvironmentVariable("RABBITMQ_URI")
			?? throw new InvalidOperationException("RABBITMQ_URI is not set");

		var factory = new ConnectionFactory { Uri = new Uri(uri) };

		_connection = await factory.CreateConnectionAsync(cancellationToken);
		_channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

		await _channel.QueueDeclareAsync(
			queue: QueueName,
			durable: true,
			exclusive: false,
			autoDelete: false,
			arguments: null,
			cancellationToken: cancellationToken
		);

		await _channel.BasicQosAsync(0, prefetchCount: 1, global: false, cancellationToken);

		var consumer = new AsyncEventingBasicConsumer(_channel);
		consumer.ReceivedAsync += OnReceivedAsync;

		await _channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, cancellationToken);

		_logger.LogInformation("Waiting for fix persisted events on {Queue}", QueueName);
	}

	private async Task OnReceivedAsync(object sender, BasicDeliverEventArgs args)
	{
		try
		{
			var evt = JsonSerializer.Deserialize<FixPersistedMessage>(args.Body.Span, JsonOptions);

			if (evt is null)
			{
				_logger.LogWarning("Bad message, dropping");
				await _channel!.BasicNackAsync(args.DeliveryTag, multiple: false, requeue: false);
				return;
			}

			_logger.LogInformation("Fix {Id} persisted", evt.Id);

			await _channel!.BasicAckAsync(args.DeliveryTag, multiple: false);
		}
		catch (JsonException je)
		{
			// In case of malformed json which might cause a poison loop
			_logger.LogError(je, "Malformed message, dropping");
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
