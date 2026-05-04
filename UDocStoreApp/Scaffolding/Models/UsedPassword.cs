using System;
using System.Collections.Generic;

namespace UDocStoreApp.Scaffolding.Models;

public partial class UsedPassword
{
    public int Id { get; set; }

    public int IdUser { get; set; }

    public string Password { get; set; }

    public DateTime Date { get; set; }

    public virtual User IdUserNavigation { get; set; }
}
