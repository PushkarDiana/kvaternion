using Kvaternion.Helpers;
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
using System.Security.Cryptography;
using System.Windows.Shapes;
using Microsoft.EntityFrameworkCore;

namespace Kvaternion
{
    /// <summary>
    /// Логика взаимодействия для LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void btnTogglePass_Click(object sender, RoutedEventArgs e)
        {
            if (txtPassword.Visibility == Visibility.Visible)
            {
                // Показываем пароль
                txtPasswordVisible.Text = txtPassword.Password;
                txtPassword.Visibility = Visibility.Collapsed;
                txtPasswordVisible.Visibility = Visibility.Visible;
                btnTogglePass.Content = "\uED1A";
            }
            else
            {
                // Скрываем пароль
                txtPassword.Password = txtPasswordVisible.Text;
                txtPasswordVisible.Visibility = Visibility.Collapsed;
                txtPassword.Visibility = Visibility.Visible;
                btnTogglePass.Content = "\uE7B3";
            }
        }

        private void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            // 1. Подготовка данных
            string loginInput = txtLogin.Text.Trim();
            string password = (txtPassword.Visibility == Visibility.Visible)
                              ? txtPassword.Password
                              : txtPasswordVisible.Text;

            if (string.IsNullOrEmpty(loginInput) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Введите логин и пароль!");
                return;
            }

            try
            {
                using (var db = new KvaternionDbContext())
                {
                    /* БЫЛО РАНЬШЕ
                    // 2. Генерируем хеш (как в базе)
                    string enteredHash = Helpers.SecurityHelper.HashPassword(password).ToLower().Trim();*/

                    // 2. Генерируем хеш

                    var enteredHash = Convert.ToBase64String(new SHA256CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(password)));

                    // 3. Выгружаем юзеров в память (ToList), чтобы избежать проблем с кодировкой SQL
                    var allUsers = db.Users.Include(u => u.Role).ToList();

                    // 4. Ищем совпадение (Игнорируем регистр логина и убираем лишние пробелы)
                    var user = allUsers.FirstOrDefault(u =>
                        u.Login == loginInput &&
                        u.PasswordHash == enteredHash);

                    if (user != null)
                    {
                        if (user.IsActive == false)
                        {
                            MessageBox.Show("Аккаунт заблокирован!");
                            return;
                        }

                        // УСПЕХ: Сохраняем и переходим дальше
                        App.CurrentUser = user;
                        MainWindow main = new MainWindow();
                        main.Show();
                        this.Close();
                    }
                    else
                    {
                        // ДИАГНОСТИКА: Почему не вошло?
                        bool loginExists = allUsers.Any(u => u.Login.Trim().Equals(loginInput, StringComparison.OrdinalIgnoreCase));

                        if (!loginExists)
                        {
                            MessageBox.Show($"Логин '{loginInput}' не найден в базе!\nПроверьте, к той ли базе подключен проект.");
                        }
                        else
                        {
                            MessageBox.Show("Логин найден, но ПАРОЛЬ неверный.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка БД: " + ex.Message);
            }
        }
    }
}
