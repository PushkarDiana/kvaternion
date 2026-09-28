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
using System.Windows.Shapes;
using Kvaternion.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Kvaternion.Pages
{
    /// <summary>
    /// Логика взаимодействия для EditStrockWindow.xaml
    /// </summary>
    public partial class EditStrockWindow : Window
    {
        private int _stockId; 
        private Stock _currentStock; 
        public EditStrockWindow(int stockId)
        {
            InitializeComponent();
            _stockId = stockId;
            LoadStockData();
        }

        private void LoadStockData()
        {
            using (var db = new KvaternionDbContext())
            {
                cbWarehouse.ItemsSource = db.Warehouses.ToList();

                var stock = db.Stocks
                    .Include(s => s.Product)
                    .Include(s => s.Warehouse)
                    .FirstOrDefault(s => s.Id == _stockId);

                if (stock != null)
                {
                    txtProductName.Text = stock.Product?.Name ?? "Неизвестно";
                    txtQuantity.Text = stock.Quantity.ToString("F2");

                    cbWarehouse.SelectedValue = stock.WarehouseId;
                }
                else
                {
                    MessageBox.Show("Ошибка: Запись об остатке не найдена.");
                    Close();
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!decimal.TryParse(txtQuantity.Text, out decimal newQuantity) || newQuantity < 0)
            {
                MessageBox.Show("Пожалуйста, введите корректное число.");
                return;
            }

            if (cbWarehouse.SelectedValue == null)
            {
                MessageBox.Show("Выберите склад.");
                return;
            }

            try
            {
                using (var db = new KvaternionDbContext())
                {
                    var stockToUpdate = db.Stocks.Find(_stockId);
                    if (stockToUpdate != null)
                    {
                        stockToUpdate.Quantity = newQuantity;
                        stockToUpdate.WarehouseId = (int)cbWarehouse.SelectedValue;

                        db.SaveChanges();
                        DialogResult = true;
                        Close();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}");
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
