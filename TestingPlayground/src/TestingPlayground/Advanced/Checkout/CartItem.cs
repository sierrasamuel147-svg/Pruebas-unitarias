namespace TestingPlayground.Advanced.Checkout;

public record CartItem(
    Guid ProductId,
    string Name,
    decimal UnitPrice,
    int Quantity
);
