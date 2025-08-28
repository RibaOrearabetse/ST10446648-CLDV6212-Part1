using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using Azure;
using Azure.Data.Tables;

namespace ABCRetailers.Models
{
    public class Product : ITableEntity
    {
        // Azure Table Storage keys
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Display(Name = "Product ID")]
        public string ProductId => RowKey;

        [Required(ErrorMessage = "Product name is required")]
        [StringLength(100)]
        [Display(Name = "Product Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000)]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Price")]
        public decimal Price { get; set; }

        // Display formatted price in Rands (ZAR)
        [NotMapped]
        public string FormattedPrice =>
            Price.ToString("C", new CultureInfo("en-ZA"));

        // Support string input/output for price binding
        public string PriceString
        {
            get => Price.ToString("F2");
            set => Price = decimal.TryParse(value, out var result) ? result : 0m;
        }

        [Required]
        [Display(Name = "Stock Available")]
        public int StockAvailable { get; set; } = 0;

        [Display(Name = "Image URL")]
        public string ImageUrl { get; set; } = string.Empty;
    }
}
