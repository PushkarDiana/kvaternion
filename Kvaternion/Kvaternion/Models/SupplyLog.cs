using System;
using System.Collections.Generic;

namespace Kvaternion.Models;

public partial class SupplyLog
{
    public int Id { get; set; }

    public int ProductId { get; set; }

    public int WarehouseId { get; set; }

    public int UserId { get; set; }

    public string OperationType { get; set; } = null!;

    public decimal Quantity { get; set; }

    public DateTime? OperationDate { get; set; }

    public string? Comment { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual Warehouse Warehouse { get; set; } = null!;
}
