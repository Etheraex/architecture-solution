package fixshared

import (
	"time"

	"github.com/google/uuid"
)

type InputMessage struct {
	Message string `json:"message" binding:"required"`
}

type Fix struct {
	ID          string    `json:"id"          bson:"_id"`
	Timestamp   time.Time `json:"timestamp"   bson:"timestamp"`
	Message     string    `json:"message"     bson:"message"`
	IsProcessed bool      `json:"isProcessed" bson:"isProcessed"`
}

type FixPersisted struct {
	ID      string `json:"id"`
	Message string `json:"message"`
}

func NewFix(message string) (Fix, error) {
	id, err := uuid.NewRandom()
	if err != nil {
		return Fix{}, err
	}

	return Fix{
		ID:        id.String(),
		Timestamp: time.Now(),
		Message:   message,
	}, nil
}
