using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace UDocStoreApp.Models
{
    [Table("Files")]
    public class FileEntity
    {
        public int id { get; set; }
        public string Name { get; set; }
        public byte[] Data { get; set; }
        public string FileHash { get; set; }
    }
}