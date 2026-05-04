using System;
using System.Collections.Generic;

namespace UDocStoreApp.Scaffolding.Models;

public partial class Right
{
    public int Id { get; set; }

    public string Name { get; set; }

    public string Description { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
