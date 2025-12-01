 namespace CarPartsShop.API.DTOs.Orders
{
    public class CreateOrderItemDto
    {
        public int PartId { get; set; }
        public int Quantity { get; set; }
    }

    // Address override ONLY when UseSavedAddress == false
    public class CreateOrderAddressOverrideDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? AddressLine1 { get; set; }
        public string? AddressLine2 { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }
        public string? Phone { get; set; }
    }

    public class CreateOrderDto
    {
        public List<CreateOrderItemDto> Items { get; set; } = new();

        public bool UseSavedAddress { get; set; } = true;

        // required when UseSavedAddress == false
        public CreateOrderAddressOverrideDto? ShippingAddressOverride { get; set; }

        public string ShippingMethod { get; set; } = "Standard";

        public string PaymentMethod { get; set; } = "Cash";

    }
}
