# ShoppingCartList (Azure Functions API)

Small Azure Functions-based HTTP API for managing shopping cart items using Cosmos DB (local emulator supported).

## Description

This repository contains a .NET 10 Azure Functions (isolated worker) HTTP API that stores shopping cart items in Azure Cosmos DB. It exposes simple CRUD endpoints to list, create, read, update, and delete items.

## Project structure

- ShoppingCartApi.cs - Function implementations (HTTP triggers)
- Program.cs - Functions host configuration and CosmosClient registration
- Models/ShoppingCartItem.cs - Domain model
- Models/CreateShoppingCartItem.cs - Request DTOs
- local.settings.json - Local configuration (not for production)

## Prerequisites

- .NET 10 SDK
- Azure Functions Core Tools (for local execution) or Visual Studio 2022/2026
- (Optional) Azure Cosmos DB Emulator for local development or an Azure Cosmos DB account

## Configuration

The Functions app reads the Cosmos DB connection string from configuration key `CosmosDbConnection`. A sample value is provided in `local.settings.json` that points to the Cosmos DB emulator:

```
"CosmosDbConnection": "AccountEndpoint=https://localhost:8081/;AccountKey=..."
```

The code expects a Cosmos DB database named `ShoppingCartItems` and a container named `Items` (these will need to exist in your Cosmos DB account/emulator).

## Run locally

1. Start the Cosmos DB emulator (if using emulator).
2. Open the solution in Visual Studio and run (F5), or run from terminal:

   dotnet build
   func start

(Ensure Azure Functions Core Tools is installed; running from Visual Studio will start the Functions host automatically.)

## API Endpoints (local)

Base URL when running locally: http://localhost:7071/api

- GET /api/shoppingcartitem
  - Returns all items

- GET /api/shoppingcartitem/{id}/{category}
  - Get single item by id and category (category used as Cosmos partition key)

- POST /api/shoppingcartitem
  - Create item
  - Body (JSON): { "itemName": "Milk", "category": "Grocery" }

- PUT /api/shoppingcartitem/{id}/{category}
  - Update item (currently only updates `collected`)
  - Body (JSON): { "collected": true }

- DELETE /api/shoppingcartitem/{id}/{category}
  - Delete item

## Examples (curl)

Create:

curl -X POST http://localhost:7071/api/shoppingcartitem -H "Content-Type: application/json" -d '{"itemName":"Apples","category":"Grocery"}'

Update:

curl -X PUT http://localhost:7071/api/shoppingcartitem/{id}/{category} -H "Content-Type: application/json" -d '{"collected":true}'

Delete:

curl -X DELETE http://localhost:7071/api/shoppingcartitem/{id}/{category}

## Notes

- local.settings.json is intended for local development only and should not be committed with production secrets.
- The project uses the configuration key `CosmosDbConnection` to build the CosmosClient.
- The model `ShoppingCartItem` contains an `Id` and `Category` fields; `Category` is used as the partition key for Cosmos DB operations.

