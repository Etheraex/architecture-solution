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

	mq := fixshared.Connect("fix_messages")
	defer mq.Close()

	deliveries, err := mq.Consume()

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
		d.Ack(false)
	}
}
