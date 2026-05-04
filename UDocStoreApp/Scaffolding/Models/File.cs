using System;
using System.Collections.Generic;

namespace UDocStoreApp.Scaffolding.Models;

public partial class File
{
    public int Id { get; set; }

    public int IdOrder { get; set; }

    public string Name { get; set; }

    public byte[] Data { get; set; }

    public virtual Order IdOrderNavigation { get; set; }
}
