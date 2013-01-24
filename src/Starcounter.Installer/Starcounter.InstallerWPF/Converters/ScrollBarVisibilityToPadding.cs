using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;

namespace Starcounter.InstallerWPF.Converters
{
    public class ScrollBarVisibilityToPadding : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            Visibility visibility = (Visibility)value;

            if (visibility == Visibility.Collapsed)
            {
                return new Thickness(0, 0, 20, 0);  // TODO: Scrollbar width
            }
            else
            {
                return new Thickness(0);
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

}
