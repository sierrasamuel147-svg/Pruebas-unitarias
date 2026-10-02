namespace TestingPlayground.Advanced.Checkout;

public interface IOrderRepository
{
    Task SaveAsync(Order order);
}
