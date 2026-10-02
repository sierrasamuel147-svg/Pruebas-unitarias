namespace TestingPlayground.Advanced.Checkout;

public interface INotificationService
{
    Task SendOrderConfirmationAsync(Order order);
}
