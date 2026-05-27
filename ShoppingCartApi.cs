using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ShoppingCartList.Models;
using System.Net;

namespace ShoppingCartList;

public class ShoppingCartApi
{
    private readonly ILogger<ShoppingCartApi> _logger;
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;

    public ShoppingCartApi(ILogger<ShoppingCartApi> logger, CosmosClient cosmosClient)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;
        _container = _cosmosClient.GetContainer("ShoppingCartItems", "Items");
    }

    [Function("GetShoppingCartItems")]
    public async Task<HttpResponseData> GetShoppingCartItems([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "shoppingcartitem")] HttpRequestData req)
    {
        _logger.LogInformation("Getting all shopping cart items.");
        List<ShoppingCartItem> _shoppingCartItems = new();
        var items = _container.GetItemQueryIterator<ShoppingCartItem>();
        while (items.HasMoreResults)
        {
            var response = await items.ReadNextAsync();
            _shoppingCartItems.AddRange(response);
        }

        var responseData = req.CreateResponse(HttpStatusCode.OK);
        await responseData.WriteAsJsonAsync(_shoppingCartItems);

        return responseData;
    }

    [Function("GetShoppingCartItem")]
    public async Task<HttpResponseData> GetShoppingCartItemById([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "shoppingcartitem/{id}/{category}")] HttpRequestData req, string id, string category)
    {
        _logger.LogInformation("Getting shopping cart item by Id:{id} and Category:{category}.", id, category);
        try
        {
            var item = await _container.ReadItemAsync<ShoppingCartItem>(id, new PartitionKey(category));
            var responseData = req.CreateResponse(HttpStatusCode.OK);
            await responseData.WriteAsJsonAsync(item.Resource);
            return responseData;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            var responseData = req.CreateResponse(HttpStatusCode.NotFound);
            await responseData.WriteAsJsonAsync("Item not found.");
            return responseData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the item by Id:{id} and Category:{category}.", id, category);
            var responseData = req.CreateResponse(HttpStatusCode.InternalServerError);
            await responseData.WriteAsJsonAsync("An error occurred while retrieving the item.");
            return responseData;
        }

    }

    [Function("CreateShoppingCartItem")]
    public async Task<HttpResponseData> CreateShoppingCartItem([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "shoppingcartitem")] HttpRequestData req)
    {
        _logger.LogInformation("Creating shopping cart item.");
        string requestData = await new StreamReader(req.Body).ReadToEndAsync();
        if (requestData is null)
        {
            var responseData = req.CreateResponse(HttpStatusCode.BadRequest);
            await responseData.WriteAsJsonAsync("Invalid item data.");
            return responseData;
        }
        var data = JsonConvert.DeserializeObject<CreateShoppingCartItem>(requestData);
        if (data == null || string.IsNullOrWhiteSpace(data.ItemName))
        {
            var responseData = req.CreateResponse(HttpStatusCode.BadRequest);
            await responseData.WriteAsJsonAsync("Invalid item data.");
            return responseData;
        }
        var newItem = new ShoppingCartItem
        {
            ItemName = data.ItemName,
            Category = data.Category,
        };
        await _container.CreateItemAsync(newItem, new PartitionKey(newItem.Category));
        var response = req.CreateResponse(HttpStatusCode.Created);
        await response.WriteAsJsonAsync(newItem);
        return response;
    }

    [Function("UpdateShoppingCartItem")]
    public async Task<HttpResponseData> UpdateShoppingCartItem([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "shoppingcartitem/{id}/{category}")] HttpRequestData req, string id, string category)
    {
        _logger.LogInformation("Updating shopping cart item by Id:{id} and Category:{category}.", id, category);

        try
        {
            var item = await _container.ReadItemAsync<ShoppingCartItem>(id, new PartitionKey(category));
            if (item.StatusCode == HttpStatusCode.NotFound)
            {
                var responseData = req.CreateResponse(HttpStatusCode.NotFound);
                await responseData.WriteAsJsonAsync("Item not found.");
                return responseData;
            }
            string requestData = await new StreamReader(req.Body).ReadToEndAsync();
            if (requestData is null)
            {
                var responseData = req.CreateResponse(HttpStatusCode.BadRequest);
                await responseData.WriteAsJsonAsync("Invalid item data.");
                return responseData;
            }
            var data = JsonConvert.DeserializeObject<UpdateShoppingCartItem>(requestData);
            if (data == null)
            {
                var responseData = req.CreateResponse(HttpStatusCode.BadRequest);
                await responseData.WriteAsJsonAsync("Invalid item data.");
                return responseData;
            }
            item.Resource.Collected = data.Collected;
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(item.Resource);
            return response;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            var responseData = req.CreateResponse(HttpStatusCode.NotFound);
            await responseData.WriteAsJsonAsync("Item not found.");
            return responseData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the item by Id:{id} and Category:{category}.", id, category);
            var responseData = req.CreateResponse(HttpStatusCode.InternalServerError);
            await responseData.WriteAsJsonAsync("An error occurred while updating the item.");
            return responseData;
        }
        
    }

    [Function("DeleteShoppingCartItem")]
    public async Task<HttpResponseData> DeleteShoppingCartItem([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "shoppingcartitem/{id}/{category}")] HttpRequestData req, string id, string category)
    {
        _logger.LogInformation("Deleting shopping cart item by Id:{id} and Category:{category}.", id, category);

        try
        {
            var item = await _container.ReadItemAsync<ShoppingCartItem>(id, new PartitionKey(category));
            if (item.StatusCode == HttpStatusCode.NotFound)
            {
                var responseData = req.CreateResponse(HttpStatusCode.NotFound);
                await responseData.WriteAsJsonAsync("Item not found.");
                return responseData;
            }
            await _container.DeleteItemAsync<ShoppingCartItem>(id, new PartitionKey(category));
            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync("Item deleted successfully.");
            return response;
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            var responseData = req.CreateResponse(HttpStatusCode.NotFound);
            await responseData.WriteAsJsonAsync("Item not found.");
            return responseData;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the item by Id:{id} and Category:{category}.", id, category);
            var responseData = req.CreateResponse(HttpStatusCode.InternalServerError);
            await responseData.WriteAsJsonAsync("An error occurred while deleting the item.");
            return responseData;
        }
        
    }
}