using Kvaternion.Models;
using System.Configuration;
using System.Data;
using System.Windows;

namespace Kvaternion
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static KvaternionDbContext Database { get; private set; } = new KvaternionDbContext();
        public static Models.User CurrentUser { get; set; }
    }
}


