package main

import (
	"fmt"
	"log/slog"
	"net/http"
	"os"
	"strings"
	"time"

	"github.com/gin-gonic/gin"
	"github.com/golang-jwt/jwt/v5"
)

type Claims struct {
	Roles []string `json:"roles"`
	jwt.RegisteredClaims
}

var secret []byte

func main() {
	logger := slog.New(slog.NewJSONHandler(os.Stdout, &slog.HandlerOptions{
		Level: slog.LevelInfo,
	})).With("service", "auth-service")

	slog.SetDefault(logger)

	secret = []byte(os.Getenv("JWT_SECRET"))
	if len(secret) == 0 {
		slog.Error("JWT_SECRET is required")
		os.Exit(1)
	}

	gin.SetMode(gin.ReleaseMode)

	router := gin.New()
	router.Use(requestLogger(), gin.Recovery())

	router.GET("/token", getToken)
	router.Any("/verify", verifyToken)

	slog.Info("auth-service active", "addr", ":8080")

	if err := router.Run(":8080"); err != nil {
		slog.Error("auth-service server failed", "err", err)
		os.Exit(1)
	}
}

func getToken(c *gin.Context) {
	sub := c.DefaultQuery("sub", "demo-user")
	now := time.Now()

	claims := Claims{
		Roles: []string{"trader"},
		RegisteredClaims: jwt.RegisteredClaims{
			Issuer:    "auth-service",
			Subject:   sub,
			IssuedAt:  jwt.NewNumericDate(now),
			NotBefore: jwt.NewNumericDate(now),
			ExpiresAt: jwt.NewNumericDate(now.Add(15 * time.Minute)),
		},
	}

	signed, err := jwt.NewWithClaims(jwt.SigningMethodHS256, claims).SignedString(secret)
	if err != nil {
		slog.Error("Signing failed", "err", err)
		c.JSON(http.StatusInternalServerError, gin.H{"error": "Could not issue token"})
		return
	}

	c.JSON(http.StatusOK, gin.H{"token": signed, "expiresIn": 900})
}

func verifyToken(c *gin.Context) {
	raw, ok := strings.CutPrefix(c.GetHeader("Authorization"), "Bearer ")
	if !ok || raw == "" {
		c.Status(http.StatusUnauthorized)
		return
	}

	claims := &Claims{}
	token, err := jwt.ParseWithClaims(raw, claims, func(t *jwt.Token) (any, error) {
		if _, ok := t.Method.(*jwt.SigningMethodHMAC); !ok {
			return nil, fmt.Errorf("Unexpected signing method: %v", t.Header["alg"])
		}
		return secret, nil
	})

	if err != nil || !token.Valid {
		slog.Warn("token rejected", "err", err)
		c.Status(http.StatusUnauthorized)
		return
	}

	c.Header("X-Auth-Subject", claims.Subject)
	c.Header("X-Auth-Roles", strings.Join(claims.Roles, ","))
	c.Status(http.StatusOK)
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
