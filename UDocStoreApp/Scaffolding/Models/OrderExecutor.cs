using System;
using System.Collections.Generic;

namespace UDocStoreApp.Scaffolding.Models;

public partial class OrderExecutor
{
    public int Id { get; set; }

    public int IdOrder { get; set; }

    public int IdExecutor { get; set; }

    public virtual Executor IdExecutorNavigation { get; set; }

    public virtual Order IdOrderNavigation { get; set; }
}
