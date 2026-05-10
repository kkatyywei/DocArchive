using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using UDocStoreApp.ViewModels;

namespace UDocStoreApp.Models
{
    [Table("User")]
    public class User 
    {
        [Key]
        public int id { get; set; }

        [Required, MaxLength(30)]
        public string Login { get; set; }

        [Required, MaxLength(50)]
        public string Name { get; set; }

        [Required, MaxLength(50)]
        public string Password { get; set; }
        public string Dol { get; set; }
        public int Active { get; set; } = 1;
        public int ChangePassword { get; set; } = 0;

        public int idRights { get; set; }
        [ForeignKey("idRights")]
        public virtual Right Right { get; set; }

        public int? idExecutor { get; set; }
        [ForeignKey("idExecutor")]
        public virtual Executor Executor { get; set; }

        public virtual ICollection<UserParam> UserParams { get; set; } = new List<UserParam>();
        public virtual ICollection<UsedPassword> UsedPasswords { get; set; } = new List<UsedPassword>();
    }
}