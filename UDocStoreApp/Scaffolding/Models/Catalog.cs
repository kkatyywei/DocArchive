using System;
using System.Collections.Generic;

namespace UDocStoreApp.Scaffolding.Models;

public partial class Catalog
{
    public int Id { get; set; }

    public string CatalogName { get; set; }

    public int IdSection { get; set; }

    public int NumberNext { get; set; }

    public virtual Section IdSectionNavigation { get; set; }

    public virtual ICollection<Order> Orders { get; set; } = new List<Order>();
}
