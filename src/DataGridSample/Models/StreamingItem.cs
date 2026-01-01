using System;

namespace DataGridSample.Models
{
    public class StreamingItem
    {
        public int Id { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public double Price { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string PriceDisplay { get; set; } = string.Empty;
        public string UpdatedAtDisplay { get; set; } = string.Empty;
    }
}
