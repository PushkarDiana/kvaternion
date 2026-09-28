using Kvaternion.Models;
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

namespace Kvaternion.Pages
{
    /// <summary>
    /// Логика взаимодействия для AddProductWindow.xaml
    /// </summary>
    public partial class AddProductWindow : Window
    {
        public AddProductWindow()
        {
            InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                using (var db = new KvaternionDbContext())
                {
                    cbCategory.ItemsSource = db.Categories.ToList();
                    cbWarehouse.ItemsSource = db.Warehouses.ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки справочников: " + ex.Message);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtArticle.Text) ||
                string.IsNullOrWhiteSpace(txtName.Text) ||
                cbCategory.SelectedItem == null ||
                cbWarehouse.SelectedItem == null)
            {
                MessageBox.Show("Заполните все обязательные поля!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!decimal.TryParse(txtInitialQuantity.Text, out decimal initialQty))
            {
                MessageBox.Show("Введите число в поле количества", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            using (var db = new KvaternionDbContext())
            {
                using (var transaction = db.Database.BeginTransaction())
                {
                    try
                    {
                        var newProduct = new Product
                        {
                            Article = txtArticle.Text.Trim(),
                            Name = txtName.Text.Trim(),
                            Unit = txtUnit.Text.Trim() ?? "шт.",
                            CategoryId = (cbCategory.SelectedItem as Category).Id
                        };
                        db.Products.Add(newProduct);
                        db.SaveChanges(); 
                        var newStock = new Stock
                        {
                            ProductId = newProduct.Id,
                            WarehouseId = (cbWarehouse.SelectedItem as Warehouse).Id,
                            Quantity = initialQty
                        };
                        db.Stocks.Add(newStock);

                        if (initialQty > 0)
                        {
                            var log = new SupplyLog
                            {
                                ProductId = newProduct.Id,
                                WarehouseId = newStock.WarehouseId,
                                UserId = 1, 
                                Quantity = initialQty,
                                OperationDate = DateTime.Now,
                                OperationType = "Поступление", 
                                Comment = "Начальное занесение в базу"
                            };
                            db.SupplyLogs.Add(log);
                        }

                        db.SaveChanges();
                        transaction.Commit();

                        MessageBox.Show("Товар успешно добавлен и занесен в журнал!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        this.DialogResult = true;
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback(); 
                        MessageBox.Show("Ошибка при сохранении: " + ex.Message, "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
