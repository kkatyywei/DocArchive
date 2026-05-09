using System.ComponentModel.DataAnnotations.Schema;

namespace UDocStoreApp.Models
{
    public class OrderFile
    {
        public int id { get; set; }
        public int idOrder { get; set; }
        public int idFile { get; set; }

        [ForeignKey("idOrder")] public virtual Order Order { get; set; }
        [ForeignKey("idFile")] public virtual FileEntity File { get; set; }
    }
}