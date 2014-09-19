using Starcounter.InstallerWPF.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Starcounter.InstallerWPF {
    /// <summary>
    /// Interaction logic for LicenseAgreement.xaml
    /// </summary>
    public partial class LicenseAgreementWindow : Window {

        #region Commands

        #region Print

        private void CanExecute_Print_Command(object sender, CanExecuteRoutedEventArgs e) {

            e.Handled = true;
            e.CanExecute = true;
        }

        private void Executed_Print_Command(object sender, ExecutedRoutedEventArgs e) {

            System.Windows.Controls.PrintDialog printDialog = new System.Windows.Controls.PrintDialog();
            FlowDocument document = this.GetFlowDocument();
            document.FontSize = 14;
            document.PageHeight = printDialog.PrintableAreaHeight;
            document.PageWidth = printDialog.PrintableAreaWidth;
            document.PagePadding = new Thickness(100, 80, 100, 50);
            document.ColumnGap = 0;
            document.ColumnWidth = printDialog.PrintableAreaWidth;
            IDocumentPaginatorSource dps = document;
            if (printDialog.ShowDialog() == true) {
                printDialog.PrintDocument(dps.DocumentPaginator, "Starcounter License Agreement");
            }
            e.Handled = true;
        }

        #endregion

        #region Close

        private void CanExecute_Close_Command(object sender, CanExecuteRoutedEventArgs e) {

            e.CanExecute = true;
            e.Handled = true;
        }

        private void Executed_Close_Command(object sender, ExecutedRoutedEventArgs e) {

            e.Handled = true;
            this.Close();
        }

        #endregion

        #endregion

        public LicenseAgreementWindow() {

            InitializeComponent();
            Loaded += new RoutedEventHandler(LicenseAgreementPage_Loaded);
        }

        void LicenseAgreementPage_Loaded(object sender, RoutedEventArgs e) {

            FlowDocument document = this.GetFlowDocument();
            this.documentholder.Document = document;
        }

        private FlowDocument GetFlowDocument() {

            string text = this.LoadText();

            HtmlToXamlConverter converter = new HtmlToXamlConverter();
            string xamlText = converter.Convert(text, typeof(string), null, System.Globalization.CultureInfo.CurrentUICulture) as String;

            FlowDocument element = System.Windows.Markup.XamlReader.Parse(xamlText) as FlowDocument;

            element.FontFamily = new FontFamily("Segoe UI");
            element.FontSize = 14;

            return element;
        }

        private string LoadText() {

            string textcontent = string.Empty;
            try {
                Assembly _assembly = Assembly.GetEntryAssembly();

                StreamReader _textStreamReader = null;
                try {
                    _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("Starcounter.InstallerWPF.resources.LicenseAgreement.html"));
                }
                catch {
                    _textStreamReader = new StreamReader(File.OpenRead(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "LicenseAgreement.html")));
                }

                string line = _textStreamReader.ReadToEnd();
                textcontent = line;
                _textStreamReader.Close();
            }
            catch (Exception) {
                //System.Windows.Forms.MessageBox.Show("Error" + e.Message);
            }
            return textcontent;
        }
    }
}
