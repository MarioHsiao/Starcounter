using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Starcounter.InstallerWPF.Converters
{
    public class IsCheckedConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            bool isEnabled = DependencyProperty.UnsetValue != values[0] && (bool)values[0];
            bool executeCommand = DependencyProperty.UnsetValue != values[1] && (bool)values[1];

            if (isEnabled == false) return false;

            return executeCommand;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {

            object[] values = new object[2];

            values[0] = null;
            values[1] = (bool)value;

            return values;

        }
    }

}
