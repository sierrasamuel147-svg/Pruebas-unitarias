namespace TestingPlayground.Advanced.Checkout;

public record Order(
    Guid Id,
    Guid CustomerId,
    IReadOnlyList<CartItem> Items,
    decimal Total
);
