using System;
using System.Collections.Generic;

namespace UDocStoreApp.Scaffolding.Models;

public partial class UserParam
{
    public int Id { get; set; }

    public int IdUser { get; set; }

    public string Name { get; set; }

    public string Value { get; set; }

    public virtual User IdUserNavigation { get; set; }
}
