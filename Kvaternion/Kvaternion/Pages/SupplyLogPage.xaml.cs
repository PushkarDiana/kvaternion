using Kvaternion.Models;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core;
using System.Linq;
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
    /// Логика взаимодействия для SupplyLogPage.xaml
    /// </summary>
    public partial class SupplyLogPage : Page
    {
        public SupplyLogPage()
        {
            InitializeComponent();
            LoadLogs();
            LoadWarehouses();
        }

        private void LoadLogs()
        {
            try
            {
                using (var db = new KvaternionDbContext())
                {
                    allLogs = (from log in db.SupplyLogs
                               join product in db.Products on log.ProductId equals product.Id
                               join warehouse in db.Warehouses on log.WarehouseId equals warehouse.Id
                               orderby log.OperationDate descending
                               select new
                               {
                                   Id = log.Id,
                                   Date = log.OperationDate ?? DateTime.Now,
                                   ProductName = product.Name,
                                   WarehouseName = warehouse.Name,
                                   Type = log.OperationType,
                                   Quantity = log.Quantity,
                                   Unit = product.Unit ?? "—"
                               }).ToList<dynamic>();
                    ApplyFilters();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки данных: " + ex.Message);
            }
        }

        private void LoadWarehouses()
        {
            using (var db = new KvaternionDbContext())
            {
                var warehouses = db.Warehouses.OrderBy(w => w.Name).ToList();
                warehouses.Insert(0, new Warehouse { Id = 0, Name = "Все объекты" }); 
                cbWarehouses.ItemsSource = warehouses;
                cbWarehouses.SelectedIndex = 0;
            }
        }

        private void dpLogs_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dpLogs.SelectedDate == null)
            {
                LoadLogs();
                return;
            }

            DateTime startDate = dpLogs.SelectedDate.Value.Date;
            DateTime endDate = startDate.AddDays(1);

            using (var db = new KvaternionDbContext())
            {
                try
                {
                    var filtered = (from log in db.SupplyLogs
                                    join product in db.Products on log.ProductId equals product.Id
                                    join warehouse in db.Warehouses on log.WarehouseId equals warehouse.Id
                                    where log.OperationDate >= startDate && log.OperationDate < endDate
                                    orderby log.OperationDate descending
                                    select new
                                    {
                                        Id = log.Id, 
                                        Date = log.OperationDate ?? DateTime.Now,
                                        ProductName = product.Name,
                                        WarehouseName = warehouse.Name,
                                        Type = log.OperationType,
                                        Quantity = log.Quantity,
                                        Unit = product.Unit ?? "—"
                                    }).ToList();

                    dgLogs.ItemsSource = filtered;
                    lblTotalLogs.Text = filtered.Count.ToString();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка фильтрации: " + ex.Message);
                }
            }
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            dpLogs.SelectedDate = null;
            txtSearch.Text = "Поиск по названию...";
            txtSearch.Foreground = Brushes.Gray;
            cbWarehouses.SelectedIndex = 0; 
            LoadLogs();
        }

        private void btnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (dgLogs.SelectedItem == null) return;

            dynamic selectedRow = dgLogs.SelectedItem;
            int logId = selectedRow.Id;
            string prodName = selectedRow.ProductName;

            var result = MessageBox.Show(
                $"Вы уверены, что хотите удалить запись о товаре \"{prodName}\"?\nОстатки на складе при этом не изменятся.",
                "Подтверждение",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var db = new KvaternionDbContext())
                    {
                        var logEntry = db.SupplyLogs.Find(logId);
                        if (logEntry != null)
                        {
                            db.SupplyLogs.Remove(logEntry);
                            db.SaveChanges();

                            if (dpLogs.SelectedDate != null)
                                dpLogs_SelectedDateChanged(null, null);
                            else
                                LoadLogs();

                            MessageBox.Show("Запись успешно удалена.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не удалось удалить: " + ex.Message);
                }
            }
        }

        private void btnAddManualLog_Click(object sender, RoutedEventArgs e)
        {
            AddSupplyLogPage addWindow = new AddSupplyLogPage();

            if (addWindow.ShowDialog() == true)
            {
                LoadLogs();
            }
        }

        private void btnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (dgLogs.SelectedItem == null) return;

            dynamic selectedRow = dgLogs.SelectedItem;
            int logId = selectedRow.Id;

            AddSupplyLogPage editWindow = new AddSupplyLogPage(logId);
            if (editWindow.ShowDialog() == true)
            {
                if (dpLogs.SelectedDate != null)
                    dpLogs_SelectedDateChanged(null, null);
                else
                    LoadLogs();
            }
        }

        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            var items = dgLogs.ItemsSource as System.Collections.IEnumerable;
            if (items == null || !items.Cast<object>().Any())
            {
                MessageBox.Show("Таблица пуста. Нечего экспортировать!", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                FileName = $"Журнал_операций_{DateTime.Now:dd_MM_yyyy}",
                DefaultExt = ".xlsx"
            };

            if (saveDialog.ShowDialog() == true)
            {
                try
                {
                    using (var workbook = new ClosedXML.Excel.XLWorkbook())
                    {
                        var worksheet = workbook.Worksheets.Add("Журнал операций");

                        // --- 1. ЗАГОЛОВОК ОТЧЕТА ---
                        var titleCell = worksheet.Cell(1, 1);
                        titleCell.Value = $"ЖУРНАЛ ОПЕРАЦИЙ ТМЦ (на {DateTime.Now:dd.MM.yyyy})";
                        titleCell.Style.Font.Bold = true;
                        titleCell.Style.Font.FontSize = 14;
                        worksheet.Range("A1:E1").Merge(); // Объединяем по количеству колонок (A-E)
                        titleCell.Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;
                        worksheet.Row(1).Height = 25;

                        // --- 2. ШАПКА ТАБЛИЦЫ ---
                        int headerRow = 3; // Начинаем с 3-й строки
                        worksheet.Cell(headerRow, 1).Value = "Дата";
                        worksheet.Cell(headerRow, 2).Value = "Тип операции";
                        worksheet.Cell(headerRow, 3).Value = "Наименование товара";
                        worksheet.Cell(headerRow, 4).Value = "Кол-во";
                        worksheet.Cell(headerRow, 5).Value = "Склад / Объект";

                        var headerRange = worksheet.Range(headerRow, 1, headerRow, 5);
                        headerRange.Style.Font.Bold = true;
                        headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;
                        headerRange.Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                        headerRange.Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

                        // --- 3. ВЫВОД ДАННЫХ ---
                        int currentRow = headerRow + 1;
                        foreach (dynamic item in items)
                        {
                            worksheet.Cell(currentRow, 1).Value = item.Date;
                            worksheet.Cell(currentRow, 2).Value = item.Type;
                            worksheet.Cell(currentRow, 3).Value = item.ProductName;
                            worksheet.Cell(currentRow, 4).Value = item.Quantity;
                            worksheet.Cell(currentRow, 5).Value = item.WarehouseName;

                            // Установка формата даты для первой колонки
                            worksheet.Cell(currentRow, 1).Style.DateFormat.Format = "dd.MM.yyyy";

                            // Границы для текущей строки
                            worksheet.Range(currentRow, 1, currentRow, 5).Style.Border.OutsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;
                            worksheet.Range(currentRow, 1, currentRow, 5).Style.Border.InsideBorder = ClosedXML.Excel.XLBorderStyleValues.Thin;

                            currentRow++;
                        }

                        // --- 4. МЕСТО ДЛЯ ПОДПИСИ ---
                        int footerRow = currentRow + 2; // Отступ 2 строки от таблицы

                        worksheet.Cell(footerRow, 1).Value = "Ответственное лицо:";
                        worksheet.Cell(footerRow, 2).Value = "__________________________";
                        worksheet.Cell(footerRow, 3).Value = "/________________/";

                        worksheet.Cell(footerRow + 1, 2).Value = "(подпись)";
                        worksheet.Cell(footerRow + 1, 2).Style.Font.FontSize = 9;
                        worksheet.Cell(footerRow + 1, 2).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                        worksheet.Cell(footerRow + 1, 3).Value = "(расшифровка)";
                        worksheet.Cell(footerRow + 1, 3).Style.Font.FontSize = 9;
                        worksheet.Cell(footerRow + 1, 3).Style.Alignment.Horizontal = ClosedXML.Excel.XLAlignmentHorizontalValues.Center;

                        // Автоподбор ширины колонок
                        worksheet.Columns().AdjustToContents();

                        workbook.SaveAs(saveDialog.FileName);
                    }

                    MessageBox.Show("Отчет успешно сформирован и сохранен!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Произошла ошибка при экспорте: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private List<dynamic> allLogs = new List<dynamic>();
        private void ApplyFilters()
        {
            string searchText = (txtSearch.Text == "Поиск по названию...") ? "" : txtSearch.Text.ToLower();

            var filtered = allLogs.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(searchText))
            {
                filtered = filtered.Where(x => x.ProductName.ToLower().Contains(searchText));
            }

            if (dpLogs.SelectedDate.HasValue)
            {
                DateTime selectedDate = dpLogs.SelectedDate.Value.Date;
                filtered = filtered.Where(x => x.Date.Date == selectedDate);
            }
            if (cbWarehouses.SelectedItem is Warehouse selectedWarehouse && selectedWarehouse.Id != 0)
            {
                filtered = filtered.Where(x => x.WarehouseName == selectedWarehouse.Name);
            }

            var resultList = filtered.ToList();
            dgLogs.ItemsSource = resultList;
            lblTotalLogs.Text = resultList.Count.ToString();
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtSearch.IsFocused || txtSearch.Text != "Поиск по названию...")
            {
                ApplyFilters();
            }
        }

        private void txtSearch_GotFocus(object sender, RoutedEventArgs e)
        {
            if (txtSearch.Text == "Поиск по названию...")
            {
                txtSearch.Text = ""; 
                txtSearch.Foreground = Brushes.Black; 
            }
        }

        private void txtSearch_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtSearch.Text))
            {
                txtSearch.Text = "Поиск по названию...";
                txtSearch.Foreground = Brushes.Gray;
            }
        }

        private void cbWarehouses_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilters();
        }
    }
}


