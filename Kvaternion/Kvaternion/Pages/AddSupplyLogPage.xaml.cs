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
using System.Data.Entity;

namespace Kvaternion.Pages
{
    /// <summary>
    /// Логика взаимодействия для AddSupplyLogPage.xaml
    /// </summary>
    public partial class AddSupplyLogPage : Window
    {
        private int? _editLogId = null;

        public AddSupplyLogPage()
        {
            InitializeComponent();
            LoadData();
            dpDate.SelectedDate = DateTime.Now;

            cbProduct.SelectionChanged += CbProduct_SelectionChanged;
        }

        public AddSupplyLogPage(int logId) : this() 
        {
            _editLogId = logId;
            FillDataForEdit();
        }

        private void LoadData()
        {
            try
            {
                using (var db = new KvaternionDbContext())
                {
                    cbProduct.ItemsSource = db.Products.OrderBy(p => p.Name).ToList();
                    cbProduct.SelectedValuePath = "Id";
                    cbProduct.DisplayMemberPath = "Name";

                    cbWarehouse.ItemsSource = db.Warehouses.OrderBy(w => w.Name).ToList();
                    cbWarehouse.SelectedValuePath = "Id";
                    cbWarehouse.DisplayMemberPath = "Name";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки справочников: " + ex.Message);
            }
        }

        private void CbProduct_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cbProduct.SelectedItem is Product selectedProduct)
            {
                lblUnit.Text = selectedProduct.Unit ?? "ед.";
            }
        }

        private void FillDataForEdit()
        {
            using (var db = new KvaternionDbContext())
            {
                var log = db.SupplyLogs.Include(l => l.Product).FirstOrDefault(l => l.Id == _editLogId);
                if (log != null)
                {
                    this.Title = "Редактирование записи";
                    foreach (ComboBoxItem item in cbType.Items)
                    {
                        if (item.Content.ToString() == log.OperationType)
                        {
                            cbType.SelectedItem = item;
                            break;
                        }
                    }

                    cbProduct.SelectedValue = log.ProductId;
                    cbWarehouse.SelectedValue = log.WarehouseId;
                    txtQuantity.Text = log.Quantity.ToString();
                    dpDate.SelectedDate = log.OperationDate;
                    txtComment.Text = log.Comment;

                    if (log.Product != null)
                        lblUnit.Text = log.Product.Unit;
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (cbProduct.SelectedValue == null || cbWarehouse.SelectedValue == null ||
                cbType.SelectedItem == null || !decimal.TryParse(txtQuantity.Text, out decimal qty))
            {
                MessageBox.Show("Пожалуйста, заполните все поля корректно!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new KvaternionDbContext())
                {
                    string opType = (cbType.SelectedItem as ComboBoxItem).Content.ToString();
                    int productId = (int)cbProduct.SelectedValue;

                    if (opType == "Расход")
                    {
                        var totalIn = db.SupplyLogs.Where(l => l.ProductId == productId && l.OperationType == "Приход").Sum(l => (decimal?)l.Quantity) ?? 0;
                        var totalOut = db.SupplyLogs.Where(l => l.ProductId == productId && l.OperationType == "Расход").Sum(l => (decimal?)l.Quantity) ?? 0; decimal currentStock = totalIn - totalOut;

                        if (_editLogId.HasValue)
                        {
                            var oldLog = db.SupplyLogs.AsNoTracking().FirstOrDefault(l => l.Id == _editLogId.Value);
                            if (oldLog != null && oldLog.OperationType == "Расход") currentStock += oldLog.Quantity;
                        }

                        if (qty > currentStock)
                        {
                            MessageBox.Show($"Недостаточно товара!\nНа складе: {currentStock}\nТребуется: {qty}", "Дефицит", MessageBoxButton.OK, MessageBoxImage.Stop);
                            return;
                        }
                    }

                    SupplyLog log;
                    if (_editLogId.HasValue)
                        log = db.SupplyLogs.Find(_editLogId.Value);
                    else
                    {
                        log = new SupplyLog();
                        db.SupplyLogs.Add(log);
                    }

                    log.ProductId = productId;
                    log.WarehouseId = (int)cbWarehouse.SelectedValue;
                    log.Quantity = qty;
                    log.OperationType = opType;
                    log.OperationDate = (dpDate.SelectedDate ?? DateTime.Today).Date; log.UserId = 1; 

                    db.SaveChanges();
                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}


