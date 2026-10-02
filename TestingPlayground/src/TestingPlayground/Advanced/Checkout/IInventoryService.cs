namespace TestingPlayground.Advanced.Checkout;

public interface IInventoryService
{
    Task<bool> HasStockAsync(
        Guid productId,
        int quantity
    );
}
