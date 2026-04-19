using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UDocStoreApp.Models
{
    [Table("UsedPassword")]
    public class UsedPassword
    {
        [Key]
        public int id { get; set; }

        public int id_User { get; set; }
        [ForeignKey("id_User")]
        public virtual User User { get; set; }

        [Required, MaxLength(50)]
        public string Password { get; set; }

        public DateTime Date { get; set; } = DateTime.Now;
    }
}