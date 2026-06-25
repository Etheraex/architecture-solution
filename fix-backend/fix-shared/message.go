package fixshared

import (
	"time"

	"github.com/google/uuid"
)

type InputMessage struct {
	Message string `json:"message" binding:"required"`
}

type Fix struct {
	ID          string     `json:"id"          bson:"_id"`
	CreatedAt   time.Time  `json:"createdAt"   bson:"createdAt"`
	CreatedBy   string     `json:"createdBy"   bson:"createdBy"`
	Message     string     `json:"message"     bson:"message"`
	IsProcessed bool       `json:"isProcessed" bson:"isProcessed"`
	ProcessedAt *time.Time `json:"processedAt" bson:"processedAt"`
}

type FixPersisted struct {
	ID      string `json:"id"`
	Message string `json:"message"`
}

func NewFix(message string, user string) (Fix, error) {
	id, err := uuid.NewRandom()
	if err != nil {
		return Fix{}, err
	}

	return Fix{
		ID:        id.String(),
		CreatedAt: time.Now(),
		CreatedBy: user,
		Message:   message,
	}, nil
}
