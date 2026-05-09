using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UDocStoreApp.Models
{
    [Table("Order")]
    public class Order
    {
        [Key]
        public int id { get; set; }

        public int idCatalog { get; set; }
        [ForeignKey("idCatalog")]
        public virtual Catalog Catalog { get; set; }

        public int NumberReg { get; set; }
        public DateTime DateOrder { get; set; }

        [MaxLength(20)]
        public string NumberOrder { get; set; }

        [MaxLength(250)]
        public string Text { get; set; }

        public int idUser { get; set; } // Автор
        [ForeignKey("idUser")]
        public virtual User Author { get; set; }

        public DateTime RegDate { get; set; } = DateTime.Now;
        public int isDel { get; set; } = 0;

        public int? idUserOpen { get; set; } // Блокировка
        [ForeignKey("idUserOpen")]
        public virtual User UserOpen { get; set; }

        public virtual ICollection<OrderExecutor> OrderExecutors { get; set; } = new List<OrderExecutor>();
        public virtual ICollection<OrderFile> OrderFiles { get; set; } = new List<OrderFile>();
    }
}