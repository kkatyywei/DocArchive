namespace UDocStoreApp.Models
{
    public class OrderFile
    {
        public int id { get; set; }
        public int idOrder { get; set; }
        public int idFile { get; set; }

        public virtual Order Order { get; set; }
        public virtual FileEntity File { get; set; }
    }
}