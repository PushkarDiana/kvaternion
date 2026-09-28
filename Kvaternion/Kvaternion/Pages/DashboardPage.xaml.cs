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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Kvaternion.Pages
{
    /// <summary>
    /// Логика взаимодействия для DashboardPage.xaml
    /// </summary>
    public partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            InitializeComponent();
            UpdateStats();
        }

        private void UpdateStats()
        {
            try
            {
                using (var db = new KvaternionDbContext())
                {
                    if (App.CurrentUser != null)
                        txtWelcome.Text = $"Добро пожаловать, {App.CurrentUser.FullName}!";
                    txtProductCount.Text = db.Products.Count().ToString();
                    txtWarehouseCount.Text = db.Warehouses.Count().ToString();

                    var today = DateTime.Today;
                    txtLogCount.Text = db.SupplyLogs.Count(l => l.OperationDate >= today).ToString();
                }
            }
            catch { /* Игнорируем ошибки для демо-режима */ }
        }

        private void btnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            AddProductWindow addProductWin = new AddProductWindow();
            addProductWin.Owner = Window.GetWindow(this);
            addProductWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (addProductWin.ShowDialog() == true)
            {
                UpdateStats(); 
            }
        }

        private void btnNewSupply_Click(object sender, RoutedEventArgs e)
        {
            AddSupplyLogPage addLogWin = new AddSupplyLogPage();

            addLogWin.Owner = Window.GetWindow(this);
            addLogWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            addLogWin.ShowDialog();

            UpdateStats(); 
        }

        private void btnReport_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new ReportsPage());
        }
    }
}
