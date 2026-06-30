package main

import (
	"context"
	"encoding/json"
	"log/slog"
	"os"
	"time"

	fixshared "github.com/Etheraex/architecture-solution/fix-backend/fix-shared"
	"go.mongodb.org/mongo-driver/v2/mongo"
)

func main() {
	logger := slog.New(slog.NewJSONHandler(os.Stdout, &slog.HandlerOptions{
		Level: slog.LevelInfo,
	})).With("service", "fix-consumer")

	slog.SetDefault(logger)

	client := connectMongo()
	defer client.Disconnect(context.Background())

	mqFixIngress := fixshared.Connect("fix_messages_ingress")
	defer mqFixIngress.Close()

	mqFixProcess := fixshared.Connect("fix_messages_process")
	defer mqFixProcess.Close()

	mqFixProcessConfirmation := fixshared.Connect("fix_messages_confirmation")
	defer mqFixProcessConfirmation.Close()

	go consumeConfirmations(mqFixProcessConfirmation)

	consumeIngress(mqFixIngress, mqFixProcess)
}

func consumeConfirmations(mq *fixshared.Client) {
	deliveries, err := mq.Consume()

	if err != nil {
		slog.Error("RabbitMQ consume failed", "queue", "fix_messages_confirmation", "err", err)
		os.Exit(1)
	}

	slog.Info("Waiting for confirmations...")

	for d := range deliveries {
		fixID := string(d.Body)

		if fixID == "" {
			slog.Warn("Empty confirmation, dropping")
			d.Nack(false, false)
			continue
		}

		ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
		matched, err := markFixProcessed(ctx, fixID)
		cancel()

		if err != nil {
			slog.Error("Mark processed failed, requeueing", "fixId", fixID, "err", err)
			d.Nack(false, true)
			continue
		}

		if matched == 0 {
			slog.Warn("Confirmation is a no-op, nacking", "fixId", fixID)
			d.Nack(false, false)
			continue
		}

		slog.Info("Fix marked processed", "fixId", fixID)

		d.Ack(false)
	}
}

func consumeIngress(mqFixIngress *fixshared.Client, mqFixProcess *fixshared.Client) {
	deliveries, err := mqFixIngress.Consume()

	if err != nil {
		slog.Error("RabbitMQ consume failed", "err", err)
		os.Exit(1)
	}

	slog.Info("Waiting for messages...")

	for d := range deliveries {
		var newFix fixshared.Fix

		if err := json.Unmarshal(d.Body, &newFix); err != nil {
			slog.Warn("Bad message, dropping", "err", err)
			d.Nack(false, false)
			continue
		}

		ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
		_, err = fixCollection.InsertOne(ctx, newFix)
		cancel()

		if err != nil {
			if mongo.IsDuplicateKeyError(err) {
				slog.Info("Duplicate fix, treating as persisted", "fixId", newFix.ID)
			} else {
				slog.Error("Insert failed, requeueing", "fixId", newFix.ID, "err", err)
				d.Nack(false, true)
				continue
			}
		} else {
			slog.Info("Stored fix", "fixId", newFix.ID)
		}

		eventBody, err := json.Marshal(fixshared.FixPersisted{
			ID:      newFix.ID,
			Message: newFix.Message,
		})

		if err != nil {
			slog.Error("Marshall failed, dropping", "fixId", newFix.ID, "err", err)
			d.Nack(false, false)
			continue
		}

		ctx, cancel = context.WithTimeout(context.Background(), 5*time.Second)
		if err := mqFixProcess.Publish(ctx, eventBody); err != nil {
			cancel()
			slog.Error("Publish persisted event failed", "fixId", newFix.ID, "err", err)
			d.Nack(false, true)
			continue
		}
		cancel()

		d.Ack(false) //inbound message confirmed, fix is persisted only after it has been successfully published for processing
	}
}
