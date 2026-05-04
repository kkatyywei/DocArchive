using System;
using System.Collections.Generic;

namespace UDocStoreApp.Scaffolding.Models;

public partial class User
{
    public int Id { get; set; }

    public string Login { get; set; }

    public string Name { get; set; }

    public string Password { get; set; }

    public int Active { get; set; }

    public int ChangePassword { get; set; }

    public int IdRights { get; set; }

    public int? IdExecutor { get; set; }

    public virtual Executor IdExecutorNavigation { get; set; }

    public virtual Right IdRightsNavigation { get; set; }

    public virtual ICollection<Order> OrderIdUserNavigations { get; set; } = new List<Order>();

    public virtual ICollection<Order> OrderIdUserOpenNavigations { get; set; } = new List<Order>();

    public virtual ICollection<UsedPassword> UsedPasswords { get; set; } = new List<UsedPassword>();

    public virtual ICollection<UserParam> UserParams { get; set; } = new List<UserParam>();
}
