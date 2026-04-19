using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UDocStoreApp.Models
{
    [Table("UserParam")]
    public class UserParam
    {
        [Key]
        public int id { get; set; }

        public int idUser { get; set; }
        [ForeignKey("idUser")]
        public virtual User User { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; }

        [MaxLength(255)]
        public string Value { get; set; }
    }
}