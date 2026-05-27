using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using ShoppingCartList.Models;
using System.Net;

namespace ShoppingCartList;

public class ShoppingCartApi
{
    private readonly ILogger<ShoppingCartApi> _logger;
    //private static List<ShoppingCartItem> _shoppingCartItems = new();
    private readonly CosmosClient _cosmosClient;
    private readonly Container _container;

    public ShoppingCartApi(ILogger<ShoppingCartApi> logger, CosmosClient cosmosClient)
    {
        _logger = logger;
        _cosmosClient = cosmosClient;
        _container = _cosmosClient.GetContainer("ShoppingCartItems", "Items");
    }

    [Function("GetShoppingCartItems")]
    public async Task<IActionResult> GetShoppingCartItems([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "shoppingcartitem")] HttpRequest req)
    {
        _logger.LogInformation("Getting all shopping cart items.");
        List<ShoppingCartItem> _shoppingCartItems = new();
        var items = _container.GetItemQueryIterator<ShoppingCartItem>();
        while (items.HasMoreResults)
        {
            var response = await items.ReadNextAsync();
            _shoppingCartItems.AddRange(response);
        }
        return new OkObjectResult(_shoppingCartItems);
    }

    [Function("GetShoppingCartItem")]
    public async Task<IActionResult> GetShoppingCartItemById([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "shoppingcartitem/{id}/{category}")] HttpRequest req, string id, string category)
    {
        _logger.LogInformation("Getting shopping cart item by Id:{id} and Category:{category}.", id, category);
        try
        {
            var item = await _container.ReadItemAsync<ShoppingCartItem>(id, new PartitionKey(category));
            return new OkObjectResult(item.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return new NotFoundObjectResult("Item not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving the item by Id:{id} and Category:{category}.", id, category);
            return new ObjectResult("An error occurred while retrieving the item.") { StatusCode = StatusCodes.Status500InternalServerError };
        }

    }

    [Function("CreateShoppingCartItem")]
    public async Task<IActionResult> CreateShoppingCartItem([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "shoppingcartitem")] HttpRequest req)
    {
        _logger.LogInformation("Creating shopping cart item.");
        string requestData = await new StreamReader(req.Body).ReadToEndAsync();
        if (requestData is null)
        {
            return new BadRequestObjectResult("Invalid item data.");
        }
        var data = JsonConvert.DeserializeObject<CreateShoppingCartItem>(requestData);
        if (data == null || string.IsNullOrWhiteSpace(data.ItemName))
        {
            return new BadRequestObjectResult("Invalid item data.");
        }
        var newItem = new ShoppingCartItem
        {
            ItemName = data.ItemName,
            Category = data.Category,
        };
        await _container.CreateItemAsync(newItem, new PartitionKey(newItem.Category));
        return new OkObjectResult(newItem);
    }

    [Function("UpdateShoppingCartItem")]
    public async Task<IActionResult> UpdateShoppingCartItem([HttpTrigger(AuthorizationLevel.Anonymous, "put", Route = "shoppingcartitem/{id}/{category}")] HttpRequest req, string id, string category)
    {
        _logger.LogInformation("Updating shopping cart item by Id:{id} and Category:{category}.", id, category);

        try
        {
            var item = await _container.ReadItemAsync<ShoppingCartItem>(id, new PartitionKey(category));
            if (item.StatusCode == HttpStatusCode.NotFound)
            {
                return new NotFoundObjectResult("Item not found.");
            }
            string requestData = await new StreamReader(req.Body).ReadToEndAsync();
            if (requestData is null)
            {
                return new BadRequestObjectResult("Invalid item data.");
            }
            var data = JsonConvert.DeserializeObject<UpdateShoppingCartItem>(requestData);
            if (data == null)
            {
                return new BadRequestObjectResult("Invalid item data.");
            }
            item.Resource.Collected = data.Collected;
            return new OkObjectResult(item.Resource);
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return new NotFoundObjectResult("Item not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the item by Id:{id} and Category:{category}.", id, category);
            return new ObjectResult("An error occurred while updating the item.") { StatusCode = StatusCodes.Status500InternalServerError };
        }
        
    }

    [Function("DeleteShoppingCartItem")]
    public async Task<IActionResult> DeleteShoppingCartItem([HttpTrigger(AuthorizationLevel.Anonymous, "delete", Route = "shoppingcartitem/{id}/{category}")] HttpRequest req, string id, string category)
    {
        _logger.LogInformation("Deleting shopping cart item by Id:{id} and Category:{category}.", id, category);

        try
        {
            var item = await _container.ReadItemAsync<ShoppingCartItem>(id, new PartitionKey(category));
            if (item.StatusCode == HttpStatusCode.NotFound)
            {
                return new NotFoundObjectResult("Item not found.");
            }
            await _container.DeleteItemAsync<ShoppingCartItem>(id, new PartitionKey(category));
            return new OkObjectResult("Item deleted successfully.");
        }
        catch (CosmosException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return new NotFoundObjectResult("Item not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the item by Id:{id} and Category:{category}.", id, category);
            return new ObjectResult("An error occurred while deleting the item.") { StatusCode = StatusCodes.Status500InternalServerError };
        }
        
    }
}