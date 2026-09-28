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
using Microsoft.EntityFrameworkCore;

namespace Kvaternion.Pages
{
    /// <summary>
    /// Логика взаимодействия для UsersPage.xaml
    /// </summary>
    public partial class UsersPage : Page
    {
        private List<UserViewModel> _allUsers;
        public UsersPage()
        {
            InitializeComponent();
            LoadUsers();
        }

        public void LoadUsers()
        {
            try
            {
                using (var db = new KvaternionDbContext())
                {
                    var users = db.Users.Include(u => u.Role).ToList();

                    _allUsers = users.Select(u => new UserViewModel
                    {
                        Id = u.Id,
                        Login = u.Login,
                        FullName = u.FullName,
                        Phone = u.Phone ?? "-",
                        Email = u.Email ?? "-",
                        RoleName = u.Role?.Name ?? "Не назначена",
                        IsActive = u.IsActive ?? true,
                        StatusText = (u.IsActive ?? true) ? "Активен" : "Заблокирован",
                        StatusColor = (u.IsActive ?? true) ? "#27AE60" : "#E74C3C"
                    }).ToList();

                    dgUsers.ItemsSource = _allUsers;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке пользователей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void txtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_allUsers == null) return;

            string search = txtSearch.Text.ToLower();
            var filtered = _allUsers.Where(u =>
                u.FullName.ToLower().Contains(search) ||
                u.Login.ToLower().Contains(search)
            ).ToList();

            dgUsers.ItemsSource = filtered;
        }

        private void btnAddUser_Click(object sender, RoutedEventArgs e)
        {
            var addWin = new Pages.AddUserWindow();
            addWin.Owner = Window.GetWindow(this);
            if (addWin.ShowDialog() == true) LoadUsers();
        }

        private void btnEditUser_Click(object sender, RoutedEventArgs e)
        {
            var selected = (sender as Button).DataContext as UserViewModel;
            if (selected != null)
            {
                var editWin = new Pages.AddUserWindow(selected.Id);
                editWin.Owner = Window.GetWindow(this);
                if (editWin.ShowDialog() == true) LoadUsers();
            }
        }

        private void btnDeleteUser_Click(object sender, RoutedEventArgs e)
        {
            var selected = (sender as Button).DataContext as UserViewModel;
            if (selected == null) return;

            var result = MessageBox.Show($"Вы уверены, что хотите удалить сотрудника {selected.FullName}?",
                "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    using (var db = new KvaternionDbContext())
                    {
                        var user = db.Users.Find(selected.Id);
                        if (user != null)
                        {
                            db.Users.Remove(user);
                            db.SaveChanges();
                            LoadUsers();
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не удалось удалить пользователя. Возможно, он связан с логами операций.");
                }
            }
        }

        public class UserViewModel
        {
            public int Id { get; set; }
            public string Login { get; set; }
            public string FullName { get; set; }
            public string RoleName { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public bool IsActive { get; set; }
            public string StatusText { get; set; }
            public string StatusColor { get; set; }
        }
    }
}

