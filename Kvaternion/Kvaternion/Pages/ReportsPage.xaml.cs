using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using LiveCharts;
using LiveCharts.Wpf;
using Kvaternion.Models;
using System.Linq;
using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace Kvaternion.Pages
{
    /// <summary>
    /// Логика взаимодействия для ReportsPage.xaml
    /// </summary>
    public partial class ReportsPage : Page
    {
        private List<string> _categoryLabels;
        public List<string> CategoryLabels
        {
            get => _categoryLabels;
            set { _categoryLabels = value; OnPropertyChanged(nameof(CategoryLabels)); }
        }

        public Func<double, string> Formatter { get; set; }

        public ReportsPage()
        {
            InitializeComponent();
            LoadFilters(); 
            LoadData();     
            this.DataContext = this;
        }

        private void LoadFilters()
        {
            using (var db = new KvaternionDbContext())
            {
                var cats = db.Categories.ToList();
                cats.Insert(0, new Category { Id = 0, Name = "Все категории" });
                cbCategoryFilter.ItemsSource = cats;
                cbCategoryFilter.SelectedIndex = 0;
                var warehouses = db.Warehouses.ToList();
                warehouses.Insert(0, new Warehouse { Id = 0, Name = "Все объекты" });
                cbWarehouseFilter.ItemsSource = warehouses;
                cbWarehouseFilter.SelectedIndex = 0;
            }
        }

        public void LoadData()
        {
            try
            {
                using (var db = new KvaternionDbContext())
                {
                    var converter = new BrushConverter();
                    var colors = new[] { "#FF8C00", "#2C3E50", "#7F8C8D", "#F39C12", "#BDC3C7" };

                    var query = db.Stocks
                        .Include(s => s.Warehouse)
                        .Include(s => s.Product)
                            .ThenInclude(p => p.Category)
                        .AsQueryable();

                    if (cbCategoryFilter.SelectedValue is int catId && catId > 0)
                    {
                        query = query.Where(s => s.Product.CategoryId == catId);
                    }

                    if (cbWarehouseFilter.SelectedValue is int warId && warId > 0)
                    {
                        query = query.Where(s => s.WarehouseId == warId);
                    }

                    var filteredList = query.ToList();
                    txtTotalQuantity.Text = filteredList.Sum(s => s.Quantity).ToString("N0");
                    txtTotalWarehouses.Text = filteredList.Select(s => s.WarehouseId).Distinct().Count().ToString();
                    txtTotalCategories.Text = filteredList.Select(s => s.Product.CategoryId).Distinct().Count().ToString();

                    var warehouseData = filteredList
                        .GroupBy(s => s.Warehouse.Name)
                        .Select(g => new { Name = g.Key, Total = (double)g.Sum(x => x.Quantity) }).ToList();

                    SeriesCollection pieSeries = new SeriesCollection();
                    int colorIdx = 0;
                    foreach (var item in warehouseData)
                    {
                        pieSeries.Add(new PieSeries
                        {
                            Title = item.Name,
                            Values = new ChartValues<double> { item.Total },
                            Fill = (Brush)converter.ConvertFrom(colors[colorIdx % colors.Length]),
                            DataLabels = true
                        });
                        colorIdx++;
                    }
                    pieChart.Series = pieSeries;

                    var categoryData = filteredList
                        .GroupBy(s => s.Product.Category.Name)
                        .Select(g => new { Name = g.Key, Total = (double)g.Sum(x => x.Quantity) })
                        .OrderBy(x => x.Total).ToList();

                    CategoryLabels = categoryData.Select(x => x.Name).ToList();
                    columnChart.Series = new SeriesCollection {
                new RowSeries {
                    Title = "Количество",
                    Values = new ChartValues<double>(categoryData.Select(x => x.Total)),
                    Fill = (Brush)converter.ConvertFrom("#FF8C00"),
                    DataLabels = true
                }
            };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка аналитики: " + ex.Message);
            }
        }
        private void Filters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadData();
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            cbCategoryFilter.SelectedIndex = 0;
            cbWarehouseFilter.SelectedIndex = 0;
            LoadData();
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}


