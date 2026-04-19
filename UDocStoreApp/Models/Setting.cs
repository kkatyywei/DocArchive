using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UDocStoreApp.Models
{
    [Table("Settings")]
    public class Setting
    {
        [Key, MaxLength(50)]
        public string Name { get; set; }

        [MaxLength(255)]
        public string Value { get; set; }
    }
}