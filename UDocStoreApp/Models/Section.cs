using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UDocStoreApp.Models
{
    [Table("Section")]
    public class Section
    {
        [Key]
        public int id { get; set; }

        [Required, MaxLength(50)]
        public string SectionName { get; set; }

        public virtual ICollection<Catalog> Catalogs { get; set; } = new List<Catalog>();
    }
}