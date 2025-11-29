    using System.ComponentModel.DataAnnotations;
    using Microsoft.EntityFrameworkCore;

    namespace CarPartsShop.API.Models
    {
        [Index(nameof(Sku), IsUnique = true)]
        public class Part
        {
            public int Id { get; set; }

            [Required, MaxLength(150)]
            public string Name { get; set; } = default!;

            [Required, MaxLength(64)]
            public string Sku { get; set; } = default!;

            [MaxLength(2000)]
            public string? Description { get; set; }

            [MaxLength(1024)]
            public string? ImageUrl { get; set; }

            [Precision(18, 2)]
            public decimal Price { get; set; }

            [Range(0, int.MaxValue)]
            public int QuantityInStock { get; set; }


            // FK
            [Required]
            public int CategoryId { get; set; }
            public Category? Category { get; set; }

            // Concurrency (optional but good)
            [Timestamp]
            public byte[]? RowVersion { get; set; }
        }
    }
