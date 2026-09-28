using System;
using System.Collections.Generic;

namespace Kvaternion.Models;

public partial class Warehouse
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public string? Address { get; set; }

    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();

    public virtual ICollection<SupplyLog> SupplyLogs { get; set; } = new List<SupplyLog>();
}
