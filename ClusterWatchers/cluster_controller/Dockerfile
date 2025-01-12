FROM golang:1.23-alpine AS builder

WORKDIR /app

COPY go.mod go.sum ./

RUN go mod download

COPY *.go ./

RUN CGO_ENABLED=0 GOOS=linux go build -o watcher

FROM alpine:3.19

WORKDIR /app

COPY --from=builder /app/watcher .

CMD ["./watcher"]