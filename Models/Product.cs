using System.ComponentModel.DataAnnotations.Schema;

namespace SqlRepositoryPOC.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ShortDescription { get; set; } = string.Empty;
    public string Colour { get; set; } = string.Empty;
    public string ColourCode { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public int? SubCategoryId { get; set; }
    public decimal Price { get; set; }
    public string Stock { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    [NotMapped]
    public List<string> Image { get; set; } = new();
}