namespace CarPartsShop.API.Models
{
    public class AdminLog
    {
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public string PerformedById { get; set; } = default!;
        public string PerformedByEmail { get; set; } = default!;
        public string Action { get; set; } = default!;
    }

}
