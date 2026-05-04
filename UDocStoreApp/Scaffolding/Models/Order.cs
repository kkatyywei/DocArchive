using System;
using System.Collections.Generic;

namespace UDocStoreApp.Scaffolding.Models;

public partial class Order
{
    public int Id { get; set; }

    public int IdCatalog { get; set; }

    public int NumberReg { get; set; }

    public DateTime DateOrder { get; set; }

    public string NumberOrder { get; set; }

    public string Text { get; set; }

    public int IdUser { get; set; }

    public DateTime RegDate { get; set; }

    public int IsDel { get; set; }

    public int? IdUserOpen { get; set; }

    public virtual ICollection<File> Files { get; set; } = new List<File>();

    public virtual Catalog IdCatalogNavigation { get; set; }

    public virtual User IdUserNavigation { get; set; }

    public virtual User IdUserOpenNavigation { get; set; }

    public virtual ICollection<OrderExecutor> OrderExecutors { get; set; } = new List<OrderExecutor>();
}
