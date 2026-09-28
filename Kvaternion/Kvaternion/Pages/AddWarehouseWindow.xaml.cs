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

namespace Kvaternion.Pages
{
    /// <summary>
    /// Логика взаимодействия для AddWarehouseWindow.xaml
    /// </summary>
    public partial class AddWarehouseWindow : Window
    {
        private int? _warehouseId = null;

        public AddWarehouseWindow(int? warehouseId = null)
        {
            InitializeComponent();
            _warehouseId = warehouseId;

            if (_warehouseId != null)
            {
                this.Title = "Редактирование объекта";
                LoadWarehouseData();
            }
        }
        private void LoadWarehouseData()
        {
            using (var db = new KvaternionDbContext())
            {
                var w = db.Warehouses.Find(_warehouseId);
                if (w != null)
                {
                    txtName.Text = w.Name;
                    txtAddress.Text = w.Address;
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text)) return;

            using (var db = new KvaternionDbContext())
            {
                if (_warehouseId == null) 
                {
                    db.Warehouses.Add(new Warehouse { Name = txtName.Text, Address = txtAddress.Text });
                }
                else 
                {
                    var w = db.Warehouses.Find(_warehouseId);
                    if (w != null)
                    {
                        w.Name = txtName.Text;
                        w.Address = txtAddress.Text;
                    }
                }
                db.SaveChanges();
            }
            this.DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

