using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UDocStoreApp.Models
{
    [Table("OrderExecutor")]
    public class OrderExecutor
    {
        [Key]
        public int id { get; set; }

        public int idOrder { get; set; }
        [ForeignKey("idOrder")]
        public virtual Order Order { get; set; }

        public int idExecutor { get; set; }
        [ForeignKey("idExecutor")]
        public virtual Executor Executor { get; set; }
    }
}