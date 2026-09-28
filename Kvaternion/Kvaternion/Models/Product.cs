using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations.Schema; 
using System.Windows.Media;

namespace Kvaternion.Models;

public partial class Product
{
    public int Id { get; set; }

    public string Article { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string Unit { get; set; } = null!;

    public int CategoryId { get; set; }

    public virtual Category Category { get; set; } = null!;

    public virtual ICollection<Stock> Stocks { get; set; } = new List<Stock>();

    public virtual ICollection<SupplyLog> SupplyLogs { get; set; } = new List<SupplyLog>();

    [NotMapped] // Чтобы база данных не пыталась создать этот столбец у себя
    public Brush StatusColor { get; set; } = Brushes.Black;
}
