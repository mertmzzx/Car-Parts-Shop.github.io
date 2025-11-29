namespace CarPartsShop.API.DTOs.Parts
{
    public class PartDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = default!;
        public string Sku { get; set; } = default!;
        public string? Description { get; set; }

        public string? ImageUrl { get; set; }

        public decimal Price { get; set; }
        public int QuantityInStock { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = default!;
    }
}
