using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UDocStoreApp.Models
{
    [Table("Files")]
    public class FileEntity
    {
        [Key]
        public int id { get; set; }

        public int idOrder { get; set; }
        [ForeignKey("idOrder")]
        public virtual Order Order { get; set; }

        [Required, MaxLength(255)]
        public string Name { get; set; }

        [Required]
        public byte[] Data { get; set; }
    }
}