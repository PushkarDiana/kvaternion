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
using System.Linq;
using Kvaternion.Helpers;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore; 

namespace Kvaternion.Pages
{
    /// <summary>
    /// Логика взаимодействия для AddUserWindow.xaml
    /// </summary>
    public partial class AddUserWindow : Window
    {
        private int? _userId = null;
        public AddUserWindow(int? userId = null)
        {
            InitializeComponent();
            _userId = userId;

            LoadRoles();

            if (_userId != null)
            {
                this.Title = "Редактирование сотрудника";
                LoadUserData();
            }
        }

        private void LoadRoles()
        {
            using (var db = new KvaternionDbContext())
            {
                cbRoles.ItemsSource = db.Roles.ToList();
            }
        }

        private void txtPhone_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string errorMessage = "";
            string pass = txtPassword.Password;

            if (string.IsNullOrEmpty(pass) && !string.IsNullOrEmpty(txtPasswordVisible.Text))
            {
                pass = txtPasswordVisible.Text;
            }

            pass = pass.Trim();

            if (string.IsNullOrWhiteSpace(txtFullName.Text)) errorMessage += "- Введите ФИО сотрудника\n";
            if (string.IsNullOrWhiteSpace(txtLogin.Text)) errorMessage += "- Введите логин\n";

            if (_userId == null && string.IsNullOrWhiteSpace(pass))
                errorMessage += "- Установите пароль для нового аккаунта\n";

            if (cbRoles.SelectedValue == null) errorMessage += "- Выберите роль доступа\n";

            string digitsOnly = new string(txtPhone.Text.Where(char.IsDigit).ToArray());
            if (digitsOnly.Length < 11) errorMessage += "- Номер телефона введен не полностью\n";

            if (!string.IsNullOrWhiteSpace(txtEmail.Text) && !txtEmail.Text.Contains("@"))
            {
                errorMessage += "- Введите корректный адрес электронной почты (должен содержать @)\n";
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                MessageBox.Show("Пожалуйста, исправьте ошибки:\n\n" + errorMessage, "Проверка данных", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                using (var db = new KvaternionDbContext())
                {
                    string login = txtLogin.Text.Trim();
                    if (db.Users.Any(u => u.Login == login && u.Id != (_userId ?? 0)))
                    {
                        MessageBox.Show("Этот логин уже занят другим сотрудником!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    User user;
                    if (_userId == null) 
                    {
                        user = new User();
                        db.Users.Add(user);
                    }
                    else 
                    {
                        user = db.Users.Include(u => u.Role).FirstOrDefault(u => u.Id == _userId);
                        if (user == null) return;
                    }

                    user.FullName = txtFullName.Text.Trim();
                    user.Login = login;
                    user.Email = txtEmail.Text.Trim();
                    user.Phone = digitsOnly; 
                    user.RoleId = (int)cbRoles.SelectedValue;
                    user.IsActive = chbIsActive.IsChecked ?? true;
                    if (!string.IsNullOrEmpty(pass))
                    {
                        user.PasswordHash = SecurityHelper.HashPassword(pass);
                    }

                    db.SaveChanges();

                    if (App.CurrentUser != null && user.Id == App.CurrentUser.Id)
                    {
                        var updatedUser = db.Users.Include(u => u.Role).FirstOrDefault(u => u.Id == user.Id);
                        App.CurrentUser = updatedUser;
                        foreach (Window window in Application.Current.Windows)
                        {
                            if (window is MainWindow mainWin) mainWin.UpdateHeader();
                        }
                    }

                    MessageBox.Show("Данные успешно сохранены в базе «Кватерион Групп»!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критическая ошибка при сохранении: {ex.Message}", "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadUserData()
        {
            using (var db = new KvaternionDbContext())
            {
                var user = db.Users.Find(_userId);
                if (user != null)
                {
                    txtFullName.Text = user.FullName;
                    txtLogin.Text = user.Login;
                    txtPhone.Text = user.Phone;
                    txtEmail.Text = user.Email;
                    cbRoles.SelectedValue = user.RoleId;
                    chbIsActive.IsChecked = user.IsActive;
                }
            }
        }

        private void btnShowPass_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            txtPasswordVisible.Text = txtPassword.Password;
            txtPassword.Visibility = Visibility.Collapsed;
            txtPasswordVisible.Visibility = Visibility.Visible;
        }

        private void btnShowPass_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            txtPassword.Password = txtPasswordVisible.Text;

            txtPasswordVisible.Visibility = Visibility.Collapsed;
            txtPassword.Visibility = Visibility.Visible;
            txtPassword.Focus();
        }

        private void txtPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (txtPasswordVisible.Visibility == Visibility.Collapsed)
                txtPasswordVisible.Text = txtPassword.Password;
        }

        private void txtPasswordVisible_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (txtPasswordVisible.Visibility == Visibility.Visible)
                txtPassword.Password = txtPasswordVisible.Text;
        }

        private void txtPhone_TextChanged(object sender, TextChangedEventArgs e)
        {
            txtPhone.TextChanged -= txtPhone_TextChanged;
            string val = new string(txtPhone.Text.Where(char.IsDigit).ToArray());

            if (val.Length > 0)
            {
                if (val.StartsWith("0") || val.StartsWith("1") || val.StartsWith("2") || val.StartsWith("3") || val.StartsWith("4") || val.StartsWith("5") || val.StartsWith("6") || val.StartsWith("8") || val.StartsWith("9"))
                    val = "7" + (val.StartsWith("7") || val.StartsWith("9") && val.Length > 10 ? val.Substring(1) : val.Length == 10 && val.StartsWith("9") ? val : "");

                StringBuilder res = new StringBuilder();
                for (int i = 0; i < val.Length && i < 11; i++)
                {
                    if (i == 1) res.Append(" (");
                    if (i == 4) res.Append(") ");
                    if (i == 7) res.Append("-");
                    if (i == 9) res.Append("-");
                    res.Append(val[i]);
                }
                txtPhone.Text = res.ToString();
                txtPhone.SelectionStart = txtPhone.Text.Length; 
            }

            txtPhone.TextChanged += txtPhone_TextChanged;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}

