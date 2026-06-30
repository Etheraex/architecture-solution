package main

import (
	"context"
	"log/slog"
	"os"
	"time"

	"go.mongodb.org/mongo-driver/v2/bson"
	"go.mongodb.org/mongo-driver/v2/mongo"
	"go.mongodb.org/mongo-driver/v2/mongo/options"
)

var fixCollection *mongo.Collection

const mongoURIEnv = "MONGO_URI"

func getMongoURI() string {
	uri := os.Getenv(mongoURIEnv)

	if uri == "" {
		slog.Error("env var is not set", "name", mongoURIEnv)
		os.Exit(1)
	}

	return uri
}

func connectMongo() *mongo.Client {
	client, err := mongo.Connect(options.Client().ApplyURI(getMongoURI()))

	if err != nil {
		slog.Error("Mongo connect failed", "err", err)
		os.Exit(1)
	}

	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)

	defer cancel()

	if err := client.Ping(ctx, nil); err != nil {
		slog.Error("Mongo ping failed", "err", err)
		os.Exit(1)
	}

	slog.Info("Connected to MongoDB")
	fixCollection = client.Database("fix").Collection("fix_messages")

	return client
}

func markFixProcessed(ctx context.Context, id string) (int64, error) {
	now := time.Now()

	res, err := fixCollection.UpdateOne(
		ctx,
		bson.M{"_id": id, "isProcessed": false},
		bson.M{"$set": bson.M{"isProcessed": true, "processedAt": now}},
	)

	if err != nil {
		return 0, err
	}

	return res.MatchedCount, nil
}
