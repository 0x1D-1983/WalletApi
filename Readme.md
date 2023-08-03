
# Wallet App

This is a wallet to store British coins and 1 pound notes.

It has two main deployables: an API and a background service.

## Wallet API

Can be used for
- Asking balance
- Adding money
- Removing money

## Credit Processor Service

Credited amounts are queued and processed in order by this background service.

# How to start the app

## Spin up infrastructure

```bash
docker compose up
```

## Run API

```bash
cd WalletApi
dotnet run
```
Open the swagger page and some money: http://localhost:5213/swagger/index.html

The first time the credit endpoint is called, will create the Kafka topic that stors the credit operations.

## Run the background service

```bash
cd CreditProcessorService
dotnet run
```
