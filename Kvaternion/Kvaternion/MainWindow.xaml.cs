using Kvaternion.Pages;
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
using System.Windows.Media.Animation;
using Microsoft.EntityFrameworkCore;

namespace Kvaternion
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            txtPageTitle.Text = "Панель управления";
            MainFrame.Navigate(new DashboardPage());

            txtPageTitle.Text = "Панель управления";
            MainFrame.Navigate(new DashboardPage());

            // Загружаем данные пользователя в шапку
            UpdateHeader();

            // Если пользователь авторизован, применяем права доступа
            if (App.CurrentUser != null)
            {
                ApplyPermissions();
            }

            RunWelcomeAnimation();
        }

        public void UpdateHeader()
        {
            if (App.CurrentUser != null)
            {
                // Принудительно сбрасываем и ставим заново
                txtUserName.Text = "";
                txtUserName.Text = App.CurrentUser.FullName;

                if (!string.IsNullOrWhiteSpace(App.CurrentUser.FullName))
                {
                    txtUserInitial.Text = App.CurrentUser.FullName.Trim()[0].ToString().ToUpper();
                }

                // Обновление фото
                if (imgUserPhoto != null)
                {
                    UpdatePhotoDisplay(App.CurrentUser.PhotoPath);
                }
            }
            // Берем данные напрямую из обновленного App.CurrentUser
            if (App.CurrentUser != null)
            {
                // Напрямую меняем свойство Text у TextBlock
                this.txtUserName.Text = App.CurrentUser.FullName;

                // Обновляем инициал
                if (!string.IsNullOrWhiteSpace(App.CurrentUser.FullName))
                {
                    this.txtUserInitial.Text = App.CurrentUser.FullName.Trim()[0].ToString().ToUpper();
                }

                // Обновление фото (если ты добавила x:Name="imgUserPhoto" в XAML)
                if (imgUserPhoto != null)
                {
                    UpdatePhotoDisplay(App.CurrentUser.PhotoPath);
                }
            }
        }

        private void UpdatePhotoDisplay(string path)
        {
            if (!string.IsNullOrEmpty(path) && System.IO.File.Exists(path))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(path);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();

                imgUserPhoto.Source = bitmap;
                imgUserPhoto.Visibility = Visibility.Visible;
                brdUserInitial.Visibility = Visibility.Collapsed;
            }
            else
            {
                imgUserPhoto.Source = null;
                imgUserPhoto.Visibility = Visibility.Collapsed;
                brdUserInitial.Visibility = Visibility.Visible;
            }
        }
        private void ApplyPermissions()
        {
            string userRole = App.CurrentUser?.Role?.Name;

            btnStorage.Visibility = Visibility.Visible;
            btnWarehouses.Visibility = Visibility.Visible;
            btnInput.Visibility = Visibility.Visible;
            btnReports.Visibility = Visibility.Visible;
            btnUsers.Visibility = Visibility.Visible;

            switch (userRole)
            {
                case "Администратор":
                    break;

                case "Бухгалтер":
                    // Бухгалтеру нужны отчеты и склад для контроля, но не настройки системы
                    btnUsers.Visibility = Visibility.Collapsed;
                    btnWarehouses.Visibility = Visibility.Collapsed;
                    break;

                case "Начальник участка":
                    // Начальник видит остатки на своих объектах и журнал, но не отчеты и персонал
                    btnReports.Visibility = Visibility.Collapsed;
                    btnUsers.Visibility = Visibility.Collapsed;
                    btnWarehouses.Visibility = Visibility.Collapsed;
                    break;

                case "Старший кладовщик":
                    // Кладовщик работает только со складом и приходом/расходом
                    btnReports.Visibility = Visibility.Collapsed;
                    btnUsers.Visibility = Visibility.Collapsed;
                    btnWarehouses.Visibility = Visibility.Collapsed;
                    break;

                default:
                    // Если роль не распознана, на всякий случай скрываем почти всё
                    btnReports.Visibility = Visibility.Collapsed;
                    btnUsers.Visibility = Visibility.Collapsed;
                    btnWarehouses.Visibility = Visibility.Collapsed;
                    break;
            }
        }
        private void RunWelcomeAnimation()
        {
            DoubleAnimation fadeOut = new DoubleAnimation
            {
                From = 1.0,
                To = 0.0,
                Duration = TimeSpan.FromSeconds(1),
                BeginTime = TimeSpan.FromSeconds(10)
            };
            fadeOut.Completed += (s, e) => tbWelcome.Visibility = Visibility.Collapsed;
            tbWelcome.BeginAnimation(TextBlock.OpacityProperty, fadeOut);
        }

        private void btnDashboard_Click(object sender, RoutedEventArgs e)
        {
            HighlightButton(sender);
            txtPageTitle.Text = "Панель управления";
            MainFrame.Navigate(new DashboardPage());
        }
        private void btnStorage_Click(object sender, RoutedEventArgs e)
        {
            HighlightButton(sender);
            txtPageTitle.Text = "Учет товарно-материальных ресурсов";
            MainFrame.Navigate(new StoragePage());
        }

        private void btnInput_Click(object sender, RoutedEventArgs e)
        {
            HighlightButton(sender);
            txtPageTitle.Text = "Поступления и выдачи";
            MainFrame.Navigate(new SupplyLogPage());
        }

        private void btnReports_Click(object sender, RoutedEventArgs e)
        {
            HighlightButton(sender);
            txtPageTitle.Text = "Аналитика и отчеты";
            MainFrame.Navigate(new Kvaternion.Pages.ReportsPage()); 
        }


        private void btnUsers_Click(object sender, RoutedEventArgs e)
        {
            HighlightButton(sender);
            txtPageTitle.Text = "Управление персоналом";
            MainFrame.Navigate(new Kvaternion.Pages.UsersPage());
        }

        private void btnProfile_Click(object sender, RoutedEventArgs e)
        {
            HighlightButton(sender);
            txtPageTitle.Text = "Личный кабинет";
            MainFrame.Navigate(new Kvaternion.Pages.ProfilePage());
        }

        private void btnInfo_Click(object sender, RoutedEventArgs e)
        {
            HighlightButton(sender);
            txtPageTitle.Text = "О системе";
            MainFrame.Navigate(new AboutPage());
        }

        private void btnLogout_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите выйти из системы?", "Выход", MessageBoxButton.YesNo,MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                LoginWindow loginWindow = new LoginWindow();
                loginWindow.Show();
                this.Close();
            }
        }

        private void btnExitApp_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Вы уверены, что хотите полностью закрыть программу?", "Завершение работы", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        private void btnWarehouses_Click(object sender, RoutedEventArgs e)
        {
            HighlightButton(sender);
            txtPageTitle.Text = "Объекты и Склады";
            MainFrame.Navigate(new Kvaternion.Pages.WarehousesPage());
        }

        private void HighlightButton(object sender)
        {
            foreach (var child in MenuStack.Children)
            {
                if (child is Button btn)
                {
                    btn.ClearValue(Button.BackgroundProperty);
                    btn.ClearValue(Button.ForegroundProperty);
                    btn.FontWeight = FontWeights.Normal;
                    if (btn.Template.FindName("AccentBorder", btn) is Border acc) acc.Visibility = Visibility.Collapsed;
                }
            }
            if (sender is Button clickedButton)
            {
                clickedButton.Foreground = Brushes.DarkOrange;
                clickedButton.FontWeight = FontWeights.Bold;
                if (clickedButton.Template.FindName("AccentBorder", clickedButton) is Border acc) acc.Visibility = Visibility.Visible;
            }
        }
    }
}