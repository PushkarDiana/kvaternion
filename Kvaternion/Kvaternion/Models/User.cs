using System;
using System.Collections.Generic;

namespace Kvaternion.Models;

public partial class User
{
    public int Id { get; set; }

    public string Login { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Phone { get; set; }

    public string? Email { get; set; }

    public string? PhotoPath { get; set; }

    public int RoleId { get; set; }

    public bool? IsActive { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual ICollection<SupplyLog> SupplyLogs { get; set; } = new List<SupplyLog>();
}
