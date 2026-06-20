package main

import (
	"context"
	"encoding/json"
	"log"
	"time"

	fixshared "github.com/Etheraex/architecture-solution/fix-backend/fix-shared"
	"go.mongodb.org/mongo-driver/v2/mongo"
)

func main() {
	client := connectMongo()
	defer client.Disconnect(context.Background())

	mqFixIngress := fixshared.Connect("fix_messages_ingress")
	defer mqFixIngress.Close()

	mqFixProcess := fixshared.Connect("fix_messages_process")
	defer mqFixProcess.Close()

	deliveries, err := mqFixIngress.Consume()

	if err != nil {
		log.Fatalf("RabbitMQ consume: %v", err.Error())
	}

	log.Printf("Waiting for messages")

	for d := range deliveries {
		var newFix fixshared.Fix

		if err := json.Unmarshal(d.Body, &newFix); err != nil {
			log.Printf("Bad message, dropping: %v", err)
			d.Nack(false, false)
			continue
		}

		ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
		_, err = fixCollection.InsertOne(ctx, newFix)
		cancel()

		if err != nil {
			if mongo.IsDuplicateKeyError(err) {
				log.Printf("Duplicate fix %s, acking", newFix.ID)
				d.Ack(false)
				continue
			}

			log.Printf("Insert failed, requeueing: %v", err.Error())
			d.Nack(false, true)
			continue
		}

		log.Printf("Stored fix %s", newFix.ID)
		d.Ack(false) // inbound message confirmed, fix is persisted

		// TODO: add retry logic for outbound queue publishing
		eventBody, err := json.Marshal(fixshared.FixPersisted{
			ID:      newFix.ID,
			Message: newFix.Message,
		})

		if err != nil {
			log.Printf("Marshalling persisted event failed %s, requeueing: %v", newFix.ID, err)
			continue
		}

		if err := mqFixProcess.Publish(context.Background(), eventBody); err != nil {
			log.Printf("Publish persisted event failed %s, requeueing: %v", newFix.ID, err)
			continue
		}

	}
}
