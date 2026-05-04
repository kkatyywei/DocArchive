using System;
using System.Collections.Generic;

namespace UDocStoreApp.Scaffolding.Models;

public partial class Executor
{
    public int Id { get; set; }

    public string Fio { get; set; }

    public string Dol { get; set; }

    public int Active { get; set; }

    public virtual ICollection<OrderExecutor> OrderExecutors { get; set; } = new List<OrderExecutor>();

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
