package main

import (
	"context"
	"log"
	"net/http"
	"time"

	"github.com/gin-gonic/gin"
)

func main() {
	client := connectMongo()
	defer client.Disconnect(context.Background())

	router := gin.Default()
	router.POST("/fix", postFix)

	router.Run("localhost:8080")
}

func postFix(c *gin.Context) {
	var inputMsg InputMessage

	if err := c.BindJSON(&inputMsg); err != nil {
		c.IndentedJSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	newId, uuiderr := getNewUUID()

	if uuiderr != nil {
		c.IndentedJSON(http.StatusInternalServerError, gin.H{"error": uuiderr.Error()})
		return
	}

	var newFix Fix
	newFix.Message = inputMsg.Message
	newFix.ID = newId
	newFix.Timestamp = time.Now()

	ctx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
	defer cancel()

	_, err := fixCollection.InsertOne(ctx, newFix)

	if err != nil {
		c.IndentedJSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}

	log.Printf("Id: %s Timestamp: %s Message: %s", newFix.ID, newFix.Timestamp, newFix.Message)

	c.IndentedJSON(http.StatusCreated, newFix)
}
