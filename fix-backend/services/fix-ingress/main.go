package main

import (
	"encoding/json"
	"log/slog"
	"net/http"
	"os"
	"time"

	fixshared "github.com/Etheraex/architecture-solution/fix-backend/fix-shared"
	"github.com/gin-gonic/gin"
)

func main() {
	logger := slog.New(slog.NewJSONHandler(os.Stdout, &slog.HandlerOptions{
		Level: slog.LevelInfo,
	})).With("service", "fix-ingress")

	slog.SetDefault(logger)

	mq := fixshared.Connect("fix_messages_ingress")
	defer mq.Close()

	gin.SetMode(gin.ReleaseMode)
	router := gin.New()
	router.Use(requestLogger(), gin.Recovery())

	router.POST("/fix", func(c *gin.Context) { postFix(c, mq) })

	slog.Info("fix-ingress listening", "addr", ":8080")

	if err := router.Run(":8080"); err != nil {
		slog.Error("fix-ingress server failed", "err", err)
		os.Exit(1)
	}
}

func postFix(c *gin.Context, mq *fixshared.Client) {
	var inputMsg fixshared.InputMessage

	if err := c.BindJSON(&inputMsg); err != nil {
		slog.Warn("Bad request body", "err", err)
		c.IndentedJSON(http.StatusBadRequest, gin.H{"error": err.Error()})
		return
	}

	newFix, err := fixshared.NewFix(inputMsg.Message)

	if err != nil {
		slog.Error("Failed to create fix", "err", err)
		c.IndentedJSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}

	body, err := json.Marshal(newFix)

	if err != nil {
		slog.Error("Failed to marshal fix", "fixId", newFix.ID, "err", err)
		c.IndentedJSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}

	if err := mq.Publish(c.Request.Context(), body); err != nil {
		slog.Error("Failed to publish fix", "fixId", newFix.ID, "err", err)
		c.IndentedJSON(http.StatusInternalServerError, gin.H{"error": err.Error()})
		return
	}

	slog.Info("Accepted fix", "fixId", newFix.ID)
	c.IndentedJSON(http.StatusAccepted, newFix)
}

func requestLogger() gin.HandlerFunc {
	return func(c *gin.Context) {
		start := time.Now()

		c.Next()

		status := c.Writer.Status()

		level := slog.LevelInfo
		switch {
		case status >= 500:
			level = slog.LevelError
		case status >= 400:
			level = slog.LevelWarn
		}

		slog.LogAttrs(c.Request.Context(), level, "HTTP request",
			slog.String("method", c.Request.Method),
			slog.String("path", c.Request.URL.Path),
			slog.Int("status", status),
			slog.Int64("durationMs", time.Since(start).Milliseconds()),
			slog.String("clientIp", c.ClientIP()),
		)
	}
}
