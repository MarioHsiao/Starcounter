﻿using System;
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
    /// Interaction logic for RemoveComponentsFinishedPage.xaml
    /// </summary>
    public partial class RemoveComponentsFinishedPage : BasePage, IFinishedPage
    {
        public bool GoToWiki {
            get {
                return false;
            }
            set {
                throw new NotImplementedException();
            }
        }
        public RemoveComponentsFinishedPage()
        {
            InitializeComponent();
        }
    }
}
