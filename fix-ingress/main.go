package main

import (
	"fmt"
	"net/http"
	"time"

	"github.com/gin-gonic/gin"
	"github.com/google/uuid"
)

// TODO: move to MongoDB
var fixList = []Fix{}

func getCurrentTimestamp() string {
	return time.Now().Format(time.RFC3339)
}

func getNewUUID() (string, error) {
	id, err := uuid.NewRandom()

	if err != nil {
		return "", err
	}

	return id.String(), nil
}

func main() {
	router := gin.Default()
	router.POST("/fix", postFix)

	router.Run("localhost:8080")
}

func getFixList(c *gin.Context) {
	c.IndentedJSON(http.StatusOK, fixList)
}

func postFix(c *gin.Context) {
	var inputMsg InputMessage

	if err := c.BindJSON(&inputMsg); err != nil {
		c.IndentedJSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	var newFix Fix
	newFix.Message = inputMsg.Message

	newId, err := getNewUUID()

	if err != nil {
		c.IndentedJSON(http.StatusInternalServerError, err.Error())
		return
	}

	newFix.ID = newId
	newFix.Timestamp = getCurrentTimestamp()

	fixList = append(fixList, newFix)

	fmt.Printf("Id: %s Timestamp: %s Message: %s", newFix.ID, newFix.Timestamp, newFix.Message)

	c.IndentedJSON(http.StatusCreated, newFix)
}
