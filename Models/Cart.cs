namespace SqlRepositoryPOC.Models;

public class Cart
{
    public int Id { get; set; }

    public int CustomerId { get; set; }

    public int TotalProductCount { get; set; }

    public decimal TotalPrice { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}