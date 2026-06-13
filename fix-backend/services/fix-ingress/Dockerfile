# ---- build stage ----
FROM golang:1.26 AS build
WORKDIR /src

# Cache deps separately from source for faster rebuilds
COPY go.mod go.sum ./
RUN go mod download

COPY . .
# Static binary so it runs on a distroless/scratch base
RUN CGO_ENABLED=0 GOOS=linux go build -o /out/fix-ingress ./services/fix-ingress

# ---- runtime stage ----
FROM gcr.io/distroless/static-debian12:nonroot
COPY --from=build /out/fix-ingress /fix-ingress
EXPOSE 8080
ENTRYPOINT ["/fix-ingress"]