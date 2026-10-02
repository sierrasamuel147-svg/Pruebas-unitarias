namespace TestingPlayground.Advanced.Checkout;

public interface IPaymentGateway
{
    Task<bool> ChargeAsync(
        Guid customerId,
        decimal amount
    );
}
