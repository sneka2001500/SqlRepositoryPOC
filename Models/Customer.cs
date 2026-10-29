namespace SqlRepositoryPOC.Models;

public class Customer
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public int? Age { get; set; }

    public string? Email { get; set; }

    public string? Password { get; set; }

    public string? Photo { get; set; }

    public string? Address { get; set; }

    public bool IsGuest { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
    public string Role { get; set; } = "User";
}