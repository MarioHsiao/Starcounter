using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows;
using Starcounter.InstallerWPF.Pages;
using Starcounter.Internal;

namespace Starcounter.InstallerWPF.Converters
{
    [ValueConversion(typeof(bool), typeof(string))]
    public class EnabledToString : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string[] arr = ((string)parameter).Split('|');

            string title = (string)arr[0];
            string subtitle = (string)arr[1];


            if ((bool)value)
            {
                return title;
            }

            return title + subtitle;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "ConvertBack function.");
        }

        #endregion
    }

    [ValueConversion(typeof(ComponentCommand), typeof(Brush))]
    public class CommandToBackgroundConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            ComponentCommand command = (ComponentCommand)value;

            switch (command)
            {

                case ComponentCommand.Install:
                    return new SolidColorBrush(Color.FromArgb(0x70, 0x20, 0xff, 0x20)); // Green

                case ComponentCommand.None:

                    break;
                case ComponentCommand.Uninstall:
                    return new SolidColorBrush(Color.FromArgb(0xff, 0xbf, 0x0e, 0x1e)); // Red

                case ComponentCommand.Update:
                    break;

            }


            return new SolidColorBrush(Color.FromArgb(0x70, 0x20, 0xff, 0x20)); // Green
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            //if ((bool)value)
            //{
            //    return Command.Install;
            //}

            //return Command.Uninstall;
            return "TODO";
        }

        #endregion
    }
  

    [ValueConversion(typeof(Boolean), typeof(Visibility))]
    public class CommandToVisibilityConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            string param = parameter as string;
            SetupOptions mode = (SetupOptions)value;

            if (!string.IsNullOrEmpty(param))
            {
                if (mode == SetupOptions.Uninstall &&
                    string.Equals("REMOVECOMPONENTS", param, StringComparison.CurrentCultureIgnoreCase))
                {
                    return Visibility.Visible;
                }
                else if (mode == SetupOptions.Install &&
                    string.Equals("ADDCOMPONENTS", param, StringComparison.CurrentCultureIgnoreCase))
                {
                    return Visibility.Visible;
                }
                else if (mode == SetupOptions.RemoveComponents &&
                    string.Equals("REMOVECOMPONENTS", param, StringComparison.CurrentCultureIgnoreCase))
                {
                    return Visibility.Visible;
                }
                else if (mode == SetupOptions.AddComponents &&
                    string.Equals("ADDCOMPONENTS", param, StringComparison.CurrentCultureIgnoreCase))
                {
                    return Visibility.Visible;
                }

                return Visibility.Collapsed;
            }

            switch (mode)
            {
                case SetupOptions.AddComponents:
                case SetupOptions.Install:
                    return Visibility.Visible;
                default:
                case SetupOptions.RemoveComponents:
                case SetupOptions.Uninstall: 
                return Visibility.Collapsed;
            }

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            if ((Visibility)value == Visibility.Visible)
            {
                return true;
            }

            return false;

        }

        #endregion
    }



}
