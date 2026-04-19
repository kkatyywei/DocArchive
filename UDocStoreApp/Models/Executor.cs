using Microsoft.VisualBasic.ApplicationServices;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UDocStoreApp.Models
{
    [Table("Executor")]
    public class Executor
    {
        [Key]
        public int id { get; set; }

        [Required, MaxLength(50)]
        public string FIO { get; set; }

        [MaxLength(100)]
        public string Dol { get; set; }

        public int Active { get; set; } = 1;

        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<OrderExecutor> OrderExecutors { get; set; } = new List<OrderExecutor>();
    }
}