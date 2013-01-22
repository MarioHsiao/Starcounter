using System;
using System.Collections.Generic;
using System.Linq;
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
using System.IO;
using Starcounter.InstallerEngine;
using Starcounter.Internal;

namespace Starcounter.InstallerWPF.Pages
{
    public partial class DeveloperToolsPage : BasePage
    {


        public override bool CanGoNext
        {
            get
            {

                Configuration config = this.DataContext as Configuration;

                if (!config.CanExecute)
                {
                    return false;
                }

                return base.CanGoNext;
            }
        }

        public DeveloperToolsPage()
        {
            InitializeComponent();
        }

    }

    public class VisualStudioInstalled : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool isInstalled;
            if ("2010".Equals((string)parameter))
            {

                isInstalled = DependenciesCheck.VStudio2010Installed();
            }
            else
            {
                isInstalled = false;
            }

            if (isInstalled)
            {
                return Visibility.Visible;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "ConvertBack function.");
        }

        #endregion
    }
}
