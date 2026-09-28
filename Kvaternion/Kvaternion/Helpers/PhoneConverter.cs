using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Kvaternion.Helpers
{
    public class PhoneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return "";

            // Оставляем только цифры
            string val = new string(value.ToString().Where(char.IsDigit).ToArray());

            if (val.Length < 11) return val; // Если номер неполный, выводим как есть

            // Форматируем: 8 (000) 000-00-00
            StringBuilder res = new StringBuilder();
            res.Append(val[0]); // Первая цифра (8 или 7)
            res.Append(" (").Append(val.Substring(1, 3)).Append(") ");
            res.Append(val.Substring(4, 3)).Append("-");
            res.Append(val.Substring(7, 2)).Append("-");
            res.Append(val.Substring(9, 2));

            return res.ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }
    }
}
