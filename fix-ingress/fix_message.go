package main

type InputMessage struct {
	Message string `json:"message"`
}

type Fix struct {
	ID          string `json:"id"`
	Timestamp   string `json:"timestamp"`
	Message     string `json:"message"`
	IsProcessed bool   `json:"isProcessed"`
}
