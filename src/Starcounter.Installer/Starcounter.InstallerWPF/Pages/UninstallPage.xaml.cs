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

namespace Starcounter.InstallerWPF.Pages
{
    /// <summary>
    /// Interaction logic for UninstallPage.xaml
    /// </summary>
    public partial class UninstallPage : BasePage
    {
        private bool _IsConfirmed;
        virtual public bool IsConfirmed
        {
            get
            {
                return _IsConfirmed;
            }
            set
            {
                if (_IsConfirmed == value) return;
                _IsConfirmed = value;
                this.OnPropertyChanged("IsConfirmed");
                this.OnPropertyChanged("CanGoNext");
            }
        }

        public override bool CanGoNext
        {
            get
            {
                return base.CanGoNext && IsConfirmed;
            }
        }

        public UninstallPage()
        {
            InitializeComponent();
        }
    }
}
