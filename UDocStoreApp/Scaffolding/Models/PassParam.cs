using System;
using System.Collections.Generic;

namespace UDocStoreApp.Scaffolding.Models;

public partial class PassParam
{
    public int Id { get; set; }

    public bool Strength { get; set; }

    public int MinWidth { get; set; }

    public bool MinWidthCheck { get; set; }

    public int MaxPeriod { get; set; }

    public bool MaxPeriodCheck { get; set; }

    public int MinPeriod { get; set; }

    public bool MinPeriodCheck { get; set; }

    public int CountLast { get; set; }

    public bool CountLastCheck { get; set; }
}
