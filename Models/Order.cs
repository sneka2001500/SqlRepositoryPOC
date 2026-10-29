namespace SqlRepositoryPOC.Models;

public class Order
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public decimal TotalPrice { get; set; }

    public string Status { get; set; } = "Placed";

    public DateTime DeliveryDate { get; set; }

    public string? ShippingAddress { get; set; }

    public string PaymentMode { get; set; } = string.Empty;

    public string? PaymentId { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}