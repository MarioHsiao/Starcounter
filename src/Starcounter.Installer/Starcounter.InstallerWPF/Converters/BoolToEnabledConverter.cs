using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Starcounter.InstallerWPF.Converters
{
    class BoolToEnabledConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool val = false;

            if (parameter != null)
            {
                if (bool.TryParse(parameter.ToString(), out val))
                {
                    if (val)
                    {
                        return !(bool)value;
                    }
                }
            }

            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "ConvertBack function.");
        }

        #endregion
    }
}
