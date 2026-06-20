package main

import (
	"encoding/json"
	"net/http"

	fixshared "github.com/Etheraex/architecture-solution/fix-backend/fix-shared"
	"github.com/gin-gonic/gin"
)

func main() {
	mq := fixshared.Connect("fix_messages_ingress")
	defer mq.Close()

	router := gin.Default()
	router.POST("/fix", func(c *gin.Context) { postFix(c, mq) })

	router.Run(":8080")
}

func postFix(c *gin.Context, mq *fixshared.Client) {
	var inputMsg fixshared.InputMessage

	if err := c.BindJSON(&inputMsg); err != nil {
		c.IndentedJSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	newFix, err := fixshared.NewFix(inputMsg.Message)

	if err != nil {
		c.IndentedJSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}

	body, err := json.Marshal(newFix)

	if err != nil {
		c.IndentedJSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}

	if err := mq.Publish(c.Request.Context(), body); err != nil {
		c.IndentedJSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}

	c.IndentedJSON(http.StatusAccepted, newFix)
}
