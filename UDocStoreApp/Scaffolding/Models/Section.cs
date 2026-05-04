using System;
using System.Collections.Generic;

namespace UDocStoreApp.Scaffolding.Models;

public partial class Section
{
    public int Id { get; set; }

    public string SectionName { get; set; }

    public virtual ICollection<Catalog> Catalogs { get; set; } = new List<Catalog>();
}
