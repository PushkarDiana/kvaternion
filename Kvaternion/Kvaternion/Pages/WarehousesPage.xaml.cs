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
using Kvaternion.Models;
using System.Linq;

namespace Kvaternion.Pages
{
    /// <summary>
    /// Логика взаимодействия для WarehousesPage.xaml
    /// </summary>
    public partial class WarehousesPage : Page
    {

        public WarehousesPage()
        {
            InitializeComponent();
            LoadWarehouses();
        }

        private void LoadWarehouses()
        {
            try
            {
                using (var db = new KvaternionDbContext())
                {
                    var warehousesData = db.Warehouses.Select(w => new
                    {
                        Id = w.Id,
                        Name = w.Name,
                        Address = w.Address ?? "Адрес не указан",

                        ItemsCount = db.Stocks.Count(s => s.WarehouseId == w.Id),
                        HasLowStock = db.Stocks.Any(s => s.WarehouseId == w.Id && s.Quantity < 5)
                    }).ToList();

                    icWarehouses.ItemsSource = warehousesData;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}",
                                "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void btnAddWarehouse_Click(object sender, RoutedEventArgs e)
        {
            var addWin = new Kvaternion.Pages.AddWarehouseWindow();

            addWin.Owner = Window.GetWindow(this);

            if (addWin.ShowDialog() == true)
            {
                LoadWarehouses(); 
            }
        }

        private void btnManage_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var selectedWarehouse = button.DataContext as dynamic;

            if (selectedWarehouse != null)
            {
                int id = selectedWarehouse.Id;
                this.NavigationService.Navigate(new StoragePage(id));
                var mainWindow = Window.GetWindow(this) as MainWindow;
                if (mainWindow != null)
                {
                    mainWindow.txtPageTitle.Text = $"Остатки: {selectedWarehouse.Name}";
                }
            }
        }

        private void btnDeleteWarehouse_Click(object sender, RoutedEventArgs e)
        {
            var selected = (sender as Button).DataContext as dynamic;
            if (selected == null) return;

            var result = MessageBox.Show($"Вы уверены, что хотите полностью удалить объект \"{selected.Name}\"?\nЭто действие удалит ВСЮ историю операций по этому складу!",
                "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var db = new KvaternionDbContext())
                    {
                        int id = selected.Id;
                        var warehouse = db.Warehouses.FirstOrDefault(w => w.Id == id);

                        if (warehouse != null)
                        {
                            bool hasRealPhysicalStocks = db.Stocks.Any(s => s.WarehouseId == id && s.Quantity > 0);

                            if (hasRealPhysicalStocks)
                            {
                                MessageBox.Show("Нельзя удалить объект, на котором числятся материалы! Сначала переместите их на другой склад.",
                                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Stop);
                                return;
                            }
                            var logs = db.SupplyLogs.Where(l => l.WarehouseId == id);
                            db.SupplyLogs.RemoveRange(logs);

                            var emptyStocks = db.Stocks.Where(s => s.WarehouseId == id);
                            db.Stocks.RemoveRange(emptyStocks);

                            db.Warehouses.Remove(warehouse);

                            db.SaveChanges();
                            LoadWarehouses(); 

                            MessageBox.Show("Объект и его история успешно удалены.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при удалении: " + ex.Message);
                }
            }
        }

        private void btnEditWarehouse_Click(object sender, RoutedEventArgs e)
        {
            var selected = (sender as Button).DataContext as dynamic;
            if (selected == null) return;

            var editWin = new Kvaternion.Pages.AddWarehouseWindow((int)selected.Id);
            editWin.Owner = Window.GetWindow(this);

            if (editWin.ShowDialog() == true)
            {
                LoadWarehouses(); 
            }
        }
    }
}

