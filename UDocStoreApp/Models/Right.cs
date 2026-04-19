using Microsoft.VisualBasic.ApplicationServices;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UDocStoreApp.Models
{
    [Table("Rights")]
    public class Right
    {
        [Key]
        public int id { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; }

        [MaxLength(200)]
        public string description { get; set; }

        public virtual ICollection<User> Users { get; set; } = new List<User>();
    }
}