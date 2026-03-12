namespace CloudBurger.Infrastructure.Notifications;
public sealed partial class PostgresNotificationService
{
    // Internal model for deserializing PostgreSQL JSON payload
    private sealed record OrderNotificationPayload
    {
        public required string Id { get; init; }
        public required string CustomerName { get; init; }
        public required string BurgerType { get; init; }
        public required int Quantity { get; init; }
        public required decimal TotalPrice { get; init; }
        public required DateTime CreatedAtUtc { get; init; }
    }
}
