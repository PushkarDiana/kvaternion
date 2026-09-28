using DocumentFormat.OpenXml.Wordprocessing;
using LiveCharts.Maps;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Security.Cryptography;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MyApp = Kvaternion.App;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Kvaternion.Pages
{
    /// <summary>
    /// Логика взаимодействия для ProfilePage.xaml
    /// </summary>
    public partial class ProfilePage : Page
    {
        private int currentUserId => Kvaternion.App.CurrentUser?.Id ?? 0;
        public ProfilePage()
            {
                InitializeComponent();
                LoadUserData();
            }

        private void LoadUserData()
        {
            try
            {
                var db = Kvaternion.App.Database;
                // currentUserId должен браться из App.CurrentUser.Id, а не быть равен 1
                var user = db.Users
                             .Include(u => u.Role) // Подгружаем связанную таблицу ролей
                             .FirstOrDefault(u => u.Id == currentUserId);

                if (user != null)
                {
                    txtFullName.Text = user.FullName;
                    txtPhone.Text = user.Phone;
                    txtEmail.Text = user.Email;

                    // Теперь это будет работать правильно:
                    txtRole.Text = user.Role?.Name ?? "Должность не назначена";

                    UpdateAvatar(user.PhotoPath);
                }
            }
            catch (Exception ex) { MessageBox.Show("Ошибка: " + ex.Message); }
        }

        private void UpdateAvatar(string path)
        {
            if (!string.IsNullOrWhiteSpace(txtFullName.Text))
                txtInitials.Text = txtFullName.Text.Trim()[0].ToString().ToUpper();

            if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                imgProfile.Source = bitmap;
                imgProfile.Visibility = Visibility.Visible;
                brdInitials.Visibility = Visibility.Collapsed;
                btnDeletePhoto.Visibility = Visibility.Visible;
            }
            else
            {
                imgProfile.Source = null;
                imgProfile.Visibility = Visibility.Collapsed;
                brdInitials.Visibility = Visibility.Visible;
                btnDeletePhoto.Visibility = Visibility.Collapsed;
            }
        }

        private void btnChangePhoto_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog = new OpenFileDialog { Filter = "Images|*.jpg;*.png;*.jpeg" };
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    string dir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Avatars");
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    string path = System.IO.Path.Combine(dir, $"u{currentUserId}{System.IO.Path.GetExtension(dialog.FileName)}");
                    File.Copy(dialog.FileName, path, true);

                    var db = Kvaternion.App.Database;
                    var user = db.Users.FirstOrDefault(u => u.Id == currentUserId);
                    if (user != null) { user.PhotoPath = path; db.SaveChanges(); }

                    LoadUserData();
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }

        private void btnDeletePhoto_Click(object sender, RoutedEventArgs e)
        {
            var db = Kvaternion.App.Database;
            var user = db.Users.FirstOrDefault(u => u.Id == currentUserId);

            if (user != null && System.IO.File.Exists(user.PhotoPath))
            {
                try
                {
                    imgProfile.Source = null;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    System.IO.File.Delete(user.PhotoPath);
                    user.PhotoPath = null;
                    db.SaveChanges();
                    LoadUserData();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не удалось удалить файл: " + ex.Message);
                }
            }
        }
        private void txtPhone_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!(sender is TextBox tb)) return;

            tb.TextChanged -= txtPhone_TextChanged;
            string val = new string(tb.Text.Where(char.IsDigit).ToArray());
            StringBuilder res = new StringBuilder();
            for (int i = 0; i < val.Length && i < 11; i++)
            {
                if (i == 1) res.Append(" (");
                if (i == 4) res.Append(") ");
                if (i == 7) res.Append("-");
                if (i == 9) res.Append("-");
                res.Append(val[i]);
            }

            tb.Text = res.ToString();
            tb.SelectionStart = tb.Text.Length;

            tb.TextChanged += txtPhone_TextChanged;
        }

        private void txtPhone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !char.IsDigit(e.Text, 0);
        }
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var db = App.Database;
                var user = db.Users.Include(u => u.Role).FirstOrDefault(u => u.Id == currentUserId);
               
                if (user != null)
                {
                    user.FullName = txtFullName.Text.Trim();
                    user.Phone = txtPhone.Text.Trim();
                    user.Email = txtEmail.Text.Trim();

                    user.Phone = new string(txtPhone.Text.Where(char.IsDigit).ToArray());

                    string oldPass = pbOldPass.Visibility == Visibility.Visible ? pbOldPass.Password : txtOldPassVisible.Text;
                    string newPass = pbNewPass.Visibility == Visibility.Visible ? pbNewPass.Password : txtNewPassVisible.Text;
                    string confirmPass = pbConfirmPass.Visibility == Visibility.Visible ? pbConfirmPass.Password : txtConfirmPassVisible.Text;

                    if (!string.IsNullOrWhiteSpace(newPass))
                    {
                        string oldPassHash = Kvaternion.Helpers.SecurityHelper.HashPassword(oldPass);
                        if (user.PasswordHash != oldPassHash)
                        {
                            MessageBox.Show("Текущий пароль введен неверно!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }

                        if (newPass != confirmPass)
                        {
                            MessageBox.Show("Новый пароль и подтверждение не совпадают!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        user.PasswordHash = Kvaternion.Helpers.SecurityHelper.HashPassword(newPass);
                    }

                    db.SaveChanges();

                    var userWithRole = db.Users
                        .Include(u => u.Role)
                        .FirstOrDefault(u => u.Id == user.Id);

                    App.CurrentUser = userWithRole;

                    foreach (Window window in Application.Current.Windows)
                    {
                        if (window is MainWindow mainWin)
                        {
                            mainWin.UpdateHeader();
                        }
                    }

                    MessageBox.Show("Данные профиля успешно обновлены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

                    LoadUserData();
                    pbOldPass.Password = txtOldPassVisible.Text = "";
                    pbNewPass.Password = txtNewPassVisible.Text = "";
                    pbConfirmPass.Password = txtConfirmPassVisible.Text = "";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Текущий пароль
        private void btnShowOldPass_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtOldPassVisible.Text = pbOldPass.Password;
            pbOldPass.Visibility = Visibility.Collapsed;
            txtOldPassVisible.Visibility = Visibility.Visible;
        }
        private void btnShowOldPass_MouseUp(object sender, MouseButtonEventArgs e)
        {
            pbOldPass.Password = txtOldPassVisible.Text;
            txtOldPassVisible.Visibility = Visibility.Collapsed;
            pbOldPass.Visibility = Visibility.Visible;
        }

        // Новый пароль
        private void btnShowNewPass_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtNewPassVisible.Text = pbNewPass.Password;
            pbNewPass.Visibility = Visibility.Collapsed;
            txtNewPassVisible.Visibility = Visibility.Visible;
        }
        private void btnShowNewPass_MouseUp(object sender, MouseButtonEventArgs e)
        {
            pbNewPass.Password = txtNewPassVisible.Text;
            txtNewPassVisible.Visibility = Visibility.Collapsed;
            pbNewPass.Visibility = Visibility.Visible;
        }

        // Подтверждение
        private void btnShowConfirmPass_MouseDown(object sender, MouseButtonEventArgs e)
        {
            txtConfirmPassVisible.Text = pbConfirmPass.Password;
            pbConfirmPass.Visibility = Visibility.Collapsed;
            txtConfirmPassVisible.Visibility = Visibility.Visible;
        }
        private void btnShowConfirmPass_MouseUp(object sender, MouseButtonEventArgs e)
        {
            pbConfirmPass.Password = txtConfirmPassVisible.Text;
            txtConfirmPassVisible.Visibility = Visibility.Collapsed;
            pbConfirmPass.Visibility = Visibility.Visible;
        }
    }
}



