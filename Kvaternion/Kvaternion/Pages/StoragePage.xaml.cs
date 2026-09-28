using Kvaternion.Models;
using Microsoft.EntityFrameworkCore;
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
using ClosedXML.Excel;
using Microsoft.Win32;

namespace Kvaternion.Pages
{
    /// <summary>
    /// Логика взаимодействия для StoragePage.xaml
    /// </summary>
    public partial class StoragePage : Page
    {
        private KvaternionDbContext _db = new KvaternionDbContext();
        private int? _preselectedWarehouseId = null;

        public StoragePage(int? warehouseId = null)
        {
            InitializeComponent();
            _preselectedWarehouseId = warehouseId;
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                var categories = _db.Categories.ToList();
                categories.Insert(0, new Category { Id = 0, Name = "Все категории" });
                cbCategory.ItemsSource = categories;
                cbCategory.SelectedIndex = 0;

                var warehouses = _db.Warehouses.ToList();
                warehouses.Insert(0, new Warehouse { Id = 0, Name = "Все склады" });
                cbWarehouse.ItemsSource = warehouses;
                cbWarehouse.SelectedIndex = 0;

                UpdateFilters(null, null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            if (_preselectedWarehouseId != null)
            {
                foreach (var item in cbWarehouse.Items)
                {
                    if (item is Warehouse w && w.Id == _preselectedWarehouseId)
                    {
                        cbWarehouse.SelectedItem = item;
                        break;
                    }
                }
            }

            UpdateFilters(null, null);
        }

        private void UpdateFilters(object sender, EventArgs e)
        {
            if (txtSearch == null || cbCategory == null || cbWarehouse == null || dgStorage == null)
                return;

            using (var db = new KvaternionDbContext())
            {
                var query = db.Stocks
                    .Include(s => s.Product)
                        .ThenInclude(p => p.Category)
                    .Include(s => s.Warehouse)
                    .AsQueryable();

                string search = txtSearch.Text.Trim().ToLower();
                if (!string.IsNullOrWhiteSpace(search))
                {
                    query = query.Where(s => s.Product.Name.ToLower().Contains(search));
                }

                if (cbCategory.SelectedItem is Category selectedCat && selectedCat.Id > 0)
                {
                    query = query.Where(s => s.Product.CategoryId == selectedCat.Id);
                }

                if (cbWarehouse.SelectedItem is Warehouse selectedWh && selectedWh.Id > 0)
                {
                    query = query.Where(s => s.WarehouseId == selectedWh.Id);
                }

                var result = query.Select(s => new
                {
                    Id = s.Id,
                    Article = s.Product.Article,
                    Name = s.Product.Name,
                    CategoryName = s.Product.Category != null ? s.Product.Category.Name : "Без категории",
                    WarehouseName = s.Warehouse.Name,
                    Quantity = s.Quantity,
                    Unit = s.Product.Unit ?? "шт.",
                    IsLowStock = s.Quantity < 5
                }).ToList();

                dgStorage.ItemsSource = result;

                lblTotalCount.Text = result.Count.ToString();
                lblLowStockCount.Text = result.Count(x => x.IsLowStock).ToString();
            }
        }

        private void btnAddProduct_Click(object sender, RoutedEventArgs e)
        {
            AddProductWindow addWin = new AddProductWindow();
            addWin.Owner = Window.GetWindow(this);

            if (addWin.ShowDialog() == true)
            {
                UpdateFilters(null, null);
                MessageBox.Show("Новый товар успешно добавлен в справочник и закреплен за складом.");
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (sender as Button).DataContext as dynamic;
            if (selectedItem != null)
            {
                EditStrockWindow editWin = new EditStrockWindow(selectedItem.Id);
                editWin.Owner = Window.GetWindow(this);
                if (editWin.ShowDialog() == true)
                {
                    UpdateFilters(null, null);
                }
            }
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (sender as Button).DataContext as dynamic;
            if (selectedItem == null) return;

            var result = MessageBox.Show($"ВНИМАНИЕ! Удаление товара '{selectedItem.Name}' приведет к удалению всей истории его движений и остатков на всех складах. Продолжить?",
                "Полное удаление", MessageBoxButton.YesNo, MessageBoxImage.Stop);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var db = new KvaternionDbContext())
                    {
                        int stockId = selectedItem.Id;
                        var stockRecord = db.Stocks.FirstOrDefault(s => s.Id == stockId);

                        if (stockRecord != null)
                        {
                            int productId = stockRecord.ProductId;

                            var logs = db.SupplyLogs.Where(l => l.ProductId == productId);
                            db.SupplyLogs.RemoveRange(logs);
                            var allStocks = db.Stocks.Where(s => s.ProductId == productId);
                            db.Stocks.RemoveRange(allStocks);
                            var product = db.Products.FirstOrDefault(p => p.Id == productId);
                            if (product != null) db.Products.Remove(product);

                            db.SaveChanges();

                            UpdateFilters(null, null);
                            MessageBox.Show("Товар и все связанные данные полностью удалены.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при полном удалении: " + ex.Message);
                }
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog()
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"Отчет_Склад_{DateTime.Now:yyyy-MM-dd}"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Остатки ТМЦ");

                        // --- 1. ЗАГОЛОВОК ОТЧЕТА ---
                        var titleCell = worksheet.Cell(1, 1);
                        titleCell.Value = $"ОТЧЕТ ПО ОСТАТКАМ ТОВАРОВ НА {DateTime.Now:dd.MM.yyyy HH:mm}";
                        titleCell.Style.Font.Bold = true;
                        titleCell.Style.Font.FontSize = 14;
                        // Объединяем ячейки под заголовок (от A1 до F1)
                        worksheet.Range("A1:F1").Merge();
                        worksheet.Row(1).Height = 25;
                        titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                        // --- 2. ШАПКА ТАБЛИЦЫ (теперь на 3-й строке) ---
                        int headerRow = 3;
                        worksheet.Cell(headerRow, 1).Value = "Артикул";
                        worksheet.Cell(headerRow, 2).Value = "Наименование";
                        worksheet.Cell(headerRow, 3).Value = "Категория";
                        worksheet.Cell(headerRow, 4).Value = "Склад";
                        worksheet.Cell(headerRow, 5).Value = "Остаток";
                        worksheet.Cell(headerRow, 6).Value = "Ед. изм.";

                        var headerRange = worksheet.Range(headerRow, 1, headerRow, 6);
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.BackgroundColor = XLColor.LightGray;
                        headerRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                        // --- 3. ДАННЫЕ ---
                        var items = dgStorage.ItemsSource as IEnumerable<dynamic>;
                        int currentRow = headerRow + 1;

                        if (items != null)
                        {
                            foreach (var item in items)
                            {
                                worksheet.Cell(currentRow, 1).Value = item.Article;
                                worksheet.Cell(currentRow, 2).Value = item.Name;
                                worksheet.Cell(currentRow, 3).Value = item.CategoryName;
                                worksheet.Cell(currentRow, 4).Value = item.WarehouseName;
                                worksheet.Cell(currentRow, 5).Value = item.Quantity;
                                worksheet.Cell(currentRow, 6).Value = item.Unit;

                                // Добавим границы для ячеек данных
                                worksheet.Range(currentRow, 1, currentRow, 6).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                currentRow++;
                            }
                        }

                        // --- 4. МЕСТО ДЛЯ ПОДПИСИ (через 2 строки после таблицы) ---
                        int signatureRow = currentRow + 2;

                        worksheet.Cell(signatureRow, 1).Value = "Ответственное лицо:";
                        worksheet.Cell(signatureRow, 2).Value = "____________________"; // Место для подписи
                        worksheet.Cell(signatureRow, 3).Value = "(ФИО)";

                        worksheet.Cell(signatureRow + 2, 1).Value = "Дата составления:";
                        worksheet.Cell(signatureRow + 2, 2).Value = DateTime.Now.ToShortDateString();

                        // Автоподбор ширины колонок
                        worksheet.Columns().AdjustToContents();

                        workbook.SaveAs(sfd.FileName);
                        MessageBox.Show("Данные успешно экспортированы!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при экспорте: " + ex.Message);
                }
            }
        }
    }
}
