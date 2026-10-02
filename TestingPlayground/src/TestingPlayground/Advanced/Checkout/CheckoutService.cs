namespace TestingPlayground.Advanced.Checkout;

public class CheckoutService
{
    private readonly IInventoryService _inventory;
    private readonly IPaymentGateway _payment;
    private readonly IOrderRepository _orders;
    private readonly INotificationService _notifications;

    public CheckoutService(
        IInventoryService inventory,
        IPaymentGateway payment,
        IOrderRepository orders,
        INotificationService notifications)
    {
        ArgumentNullException.ThrowIfNull(inventory);
        ArgumentNullException.ThrowIfNull(payment);
        ArgumentNullException.ThrowIfNull(orders);
        ArgumentNullException.ThrowIfNull(notifications);

        _inventory = inventory;
        _payment = payment;
        _orders = orders;
        _notifications = notifications;
    }

    public async Task<Order> CheckoutAsync(Guid customerId, IReadOnlyList<CartItem> items)
    {
        ValidateCart(items);

        await EnsureStockAsync(items);

        var total = CalculateTotal(items);

        if (!await _payment.ChargeAsync(customerId, total))
        {
            throw new InvalidOperationException("The payment was rejected.");
        }

        var order = new Order(Guid.NewGuid(), customerId, items.ToList().AsReadOnly(), total);

        await _orders.SaveAsync(order);
        await _notifications.SendOrderConfirmationAsync(order);

        return order;
    }

    private static void ValidateCart(IReadOnlyList<CartItem> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count == 0)
        {
            throw new ArgumentException("The cart cannot be empty.", nameof(items));
        }

        foreach (var item in items)
        {
            if (item is null)
            {
                throw new ArgumentException("The cart cannot contain null items.", nameof(items));
            }

            if (item.Quantity <= 0)
            {
                throw new ArgumentException($"Quantity for '{item.Name}' must be greater than zero.", nameof(items));
            }

            if (item.UnitPrice < 0)
            {
                throw new ArgumentException($"Unit price for '{item.Name}' cannot be negative.", nameof(items));
            }
        }
    }

    private async Task EnsureStockAsync(IReadOnlyList<CartItem> items)
    {
        foreach (var item in items)
        {
            if (!await _inventory.HasStockAsync(item.ProductId, item.Quantity))
            {
                throw new InvalidOperationException($"Insufficient stock for '{item.Name}'.");
            }
        }
    }

    private static decimal CalculateTotal(IEnumerable<CartItem> items) =>
        items.Sum(item => item.UnitPrice * item.Quantity);
}
