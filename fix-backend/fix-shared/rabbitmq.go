package fixshared

import (
	"context"
	"fmt"
	"log/slog"
	"os"
	"time"

	amqp "github.com/rabbitmq/amqp091-go"
)

const amqpURIEnv = "RABBITMQ_URI"

func getRabbitMqURI() string {
	uri := os.Getenv(amqpURIEnv)

	if uri == "" {
		slog.Error("env var not set", "name", amqpURIEnv)
		os.Exit(1)
	}

	return uri
}

type Client struct {
	connection *amqp.Connection
	channel    *amqp.Channel
	queue      string
	returns    <-chan amqp.Return
}

func Connect(queue string) *Client {
	connection, err := amqp.Dial(getRabbitMqURI())

	if err != nil {
		slog.Error("RabbitMQ dial failed", "err", err)
		os.Exit(1)
	}

	channel, err := connection.Channel()

	if err != nil {
		slog.Error("RabbitMQ channel failed", "err", err)
		os.Exit(1)
	}

	// Confirm broker received messages
	if err := channel.Confirm(false); err != nil {
		slog.Error("RabbitMQ confirm mode failed", "err", err)
		os.Exit(1)
	}

	returns := channel.NotifyReturn(make(chan amqp.Return, 1))

	_, err = channel.QueueDeclare(queue, true, false, false, false, nil)

	if err != nil {
		slog.Error("RabbitMQ queue declare failed", "err", err)
		os.Exit(1)
	}

	slog.Info("Connected to RabbitMQ", "queue", queue)

	return &Client{connection: connection, channel: channel, queue: queue, returns: returns}
}

func (c *Client) Close() {
	if c.channel != nil {
		c.channel.Close()
	}

	if c.connection != nil {
		c.connection.Close()
	}
}

func (c *Client) Publish(ctx context.Context, body []byte) error {
	// Specific publish method which actually uses the confirm mode activated when creating the channel
	conf, err := c.channel.PublishWithDeferredConfirmWithContext(
		ctx,
		"",
		c.queue,
		true, /* mandatory flag */
		false,
		amqp.Publishing{
			ContentType:  "application/json",
			DeliveryMode: amqp.Persistent,
			Timestamp:    time.Now(),
			Body:         body,
		})

	if err != nil {
		return err
	}

	ok, err := conf.WaitContext(ctx)
	if err != nil {
		return err
	}
	if !ok {
		return fmt.Errorf("Publish nacked by broker, queue %q", c.queue)
	}

	select {
	case <-c.returns:
		return fmt.Errorf("Message unroutable, queue %q", c.queue)
	default:
	}

	return nil
}

func (c *Client) Consume() (<-chan amqp.Delivery, error) {
	if err := c.channel.Qos(1, 0, false); err != nil {
		return nil, err
	}

	return c.channel.Consume(c.queue, "", false, false, false, false, nil)
}
