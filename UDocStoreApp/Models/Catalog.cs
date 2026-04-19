using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Windows.Controls;

namespace UDocStoreApp.Models
{
    [Table("Catalog")]
    public class Catalog
    {
        [Key]
        public int id { get; set; }

        [Required, MaxLength(50)]
        public string CatalogName { get; set; }

        public int idSection { get; set; }
        [ForeignKey("idSection")]
        public virtual Section Section { get; set; }

        public int NumberNext { get; set; } = 1;
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
    }
}