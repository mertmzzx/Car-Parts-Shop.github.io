namespace CarPartsShop.API.DTOs.Admin
{
    public class AdminLogDto
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string UserEmail { get; set; } = default!;
        public string Action { get; set; } = default!;
    }
}
