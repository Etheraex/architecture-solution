package main

import (
	"context"
	"log"
	"os"
	"time"

	"go.mongodb.org/mongo-driver/v2/mongo"
	"go.mongodb.org/mongo-driver/v2/mongo/options"
)

var fixCollection *mongo.Collection

const mongoURIEnv = "MONGO_URI"

func getMongoURI() string {
	uri := os.Getenv(mongoURIEnv)

	if uri == "" {
		log.Fatalf("%s is not set", mongoURIEnv)
	}

	return uri
}

func connectMongo() *mongo.Client {
	client, err := mongo.Connect(options.Client().ApplyURI(getMongoURI()))

	if err != nil {
		log.Fatalf("Mongo connect: %v", err)
	}

	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)

	defer cancel()

	if err := client.Ping(ctx, nil); err != nil {
		log.Fatalf("Mongo ping: %v", err)
	}

	log.Println("Connected to MongoDB")
	fixCollection = client.Database("fix").Collection("fix_messages")

	return client
}
