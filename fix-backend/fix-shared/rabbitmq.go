package fixshared

import (
	"context"
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

	_, err = channel.QueueDeclare(queue, true, false, false, false, nil)

	if err != nil {
		slog.Error("RabbitMQ queue declare failed", "err", err)
		os.Exit(1)
	}

	slog.Info("Connected to RabbitMQ", "queue", queue)

	return &Client{connection: connection, channel: channel, queue: queue}
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
	return c.channel.PublishWithContext(ctx, "", c.queue, false, false, amqp.Publishing{
		ContentType:  "application/json",
		DeliveryMode: amqp.Persistent,
		Timestamp:    time.Now(),
		Body:         body,
	})
}

func (c *Client) Consume() (<-chan amqp.Delivery, error) {
	if err := c.channel.Qos(1, 0, false); err != nil {
		return nil, err
	}

	return c.channel.Consume(c.queue, "", false, false, false, false, nil)
}
