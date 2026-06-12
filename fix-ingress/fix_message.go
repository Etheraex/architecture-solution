package main

import "time"

type InputMessage struct {
	Message string `json:"message" binding:"required"`
}

type Fix struct {
	ID          string    `json:"id"              bson:"_id"`
	Timestamp   time.Time `json:"timestamp"       bson:"timestamp"`
	Message     string    `json:"message"         bson:"message"`
	IsProcessed bool      `json:"isProcessed"     bson:"isProcessed"`
}
