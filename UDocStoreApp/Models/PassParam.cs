using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UDocStoreApp.Models
{
    [Table("PassParam")]
    public class PassParam
    {
        [Key]
        public int id { get; set; } = 1;

        public bool Strength { get; set; }
        public int MinWidth { get; set; }
        public bool MinWidthCheck { get; set; }
        public int MaxPeriod { get; set; }
        public bool MaxPeriodCheck { get; set; }
        public int MinPeriod { get; set; }
        public bool MinPeriodCheck { get; set; }
        public int CountLast { get; set; }
        public bool CountLastCheck { get; set; }
    }
}