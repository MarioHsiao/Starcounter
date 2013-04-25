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
using System.Resources;
using System.Reflection;
using System.Windows.Forms;
using Starcounter.InstallerWPF.Converters;

namespace Starcounter.InstallerWPF.Pages
{
    /// <summary>
    /// Interaction logic for Page1.xaml
    /// </summary>
    public partial class LicenseAgreementPage : BasePage
    {
        #region Commands

        #region Print

        private void CanExecute_Print_Command(object sender, CanExecuteRoutedEventArgs e)
        {
            //e.CanExecute = myBrowser != null && (myBrowser.Document as mshtml.IHTMLDocument2) != null;
            e.Handled = true;
            e.CanExecute = true;
        }
        private void Executed_Print_Command(object sender, ExecutedRoutedEventArgs e)
        {
            //mshtml.IHTMLDocument2 doc = myBrowser.Document as mshtml.IHTMLDocument2;
            //doc.execCommand("Print", true, null);

            System.Windows.Controls.PrintDialog printDialog = new System.Windows.Controls.PrintDialog();

            FlowDocument document = this.GetFlowDocument();

            document.FontSize = 14;
            document.PageHeight = printDialog.PrintableAreaHeight;
            document.PageWidth = printDialog.PrintableAreaWidth;
            document.PagePadding = new Thickness(100,80,100,50);
            document.ColumnGap = 0;
            document.ColumnWidth = printDialog.PrintableAreaWidth;
            IDocumentPaginatorSource dps = document;
            if (printDialog.ShowDialog() == true)
            {
                printDialog.PrintDocument(dps.DocumentPaginator, "Starcounter License Agreement");
            }


            e.Handled = true;
        }

        #endregion

        #endregion

        #region Properties
        public override bool CanGoNext
        {
            get
            {
                return true;
            }
        }
        #endregion

        public LicenseAgreementPage()
        {
            InitializeComponent();
            Loaded += new RoutedEventHandler(LicenseAgreementPage_Loaded);
        }

        void LicenseAgreementPage_Loaded(object sender, RoutedEventArgs e)
        {
            //this.LoadText();
            //FlowDocumentScrollViewer d = new FlowDocumentScrollViewer();


            FlowDocument document = this.GetFlowDocument();


            //            HtmlToXamlConverter converter = new HtmlToXamlConverter();
            //            string xamlText = converter.Convert(text, typeof(string), null, System.Globalization.CultureInfo.CurrentUICulture) as String;

            //            FlowDocument element = System.Windows.Markup.XamlReader.Parse(xamlText) as FlowDocument;
            this.documentholder.Document = document;
        }

        private FlowDocument GetFlowDocument()
        {
            string text = this.LoadText();

            HtmlToXamlConverter converter = new HtmlToXamlConverter();
            string xamlText = converter.Convert(text, typeof(string), null, System.Globalization.CultureInfo.CurrentUICulture) as String;

            FlowDocument element = System.Windows.Markup.XamlReader.Parse(xamlText) as FlowDocument;

            element.FontFamily = new FontFamily("Segoe UI");
            element.FontSize = 14;

            return element;
        }


        //private void LoadText()
        //{
        //    try
        //    {
        //        Assembly _assembly = Assembly.GetEntryAssembly();

        //        StreamReader _textStreamReader = null;
        //        try
        //        {
        //            _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("Starcounter.InstallerWPF.resources.LicenseAgreement.html"));
        //        }
        //        catch
        //        {
        //            _textStreamReader = new StreamReader(File.OpenRead(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "LicenseAgreement.html")));
        //        }

        //        string line = _textStreamReader.ReadToEnd();

        //        // Fixing the "Style" (font size)
        //        //string style = "<style type=\"text/css\">h1 {font-family:Calibri;font-size:26px;line-height:100%;padding-left:12px}h2 {font-family:Calibri;font-size:24px;line-height:100%;padding-left:12px}p {font-family:Calibri;font-size:18px;line-height:100%;padding-left:12px}</style>";

        //        // Account for the DPI scaling of this monitor.
        //        Matrix m = PresentationSource.FromVisual(this).CompositionTarget.TransformToDevice;
        //        double dpiX = m.M11;
        //        double dpiY = m.M22;

        //        string style = string.Empty;

        //        // Fixing the "Style" (font size)
        //        if (dpiX > 1.0 || dpiY > 1.0)   // For 120 DPI, the values are 1.25.
        //        {

        //            style = "<style type=\"text/css\">h1 {font-family:Calibri;font-size:26px;line-height:100%;padding-left:12px}h2 {font-family:Calibri;font-size:24px;line-height:100%;padding-left:12px}p {font-family:Calibri;font-size:18px;line-height:100%;padding-left:12px}</style>";
        //        }
        //        else // 1.0 = 96 DPI
        //        {
        //            // The font sizes are to big for normal resolutions (DPI) we need to scale them down a bit.
        //            style = "<style type=\"text/css\">h1 {font-family:Calibri;font-size:22px;line-height:100%;padding-left:12px}h2 {font-family:Calibri;font-size:15px;line-height:100%;padding-left:12px}p {font-family:Calibri;font-size:12px;line-height:100%;padding-left:12px}</style>";
        //        }



        //        line = line.Replace("<head>", "<head>" + style);

        //        _textStreamReader.Close();

        //        this.myBrowser.NavigateToString(line);

        //    }
        //    catch (Exception e)
        //    {
        //        System.Windows.Forms.MessageBox.Show("Error" + e.Message);
        //    }
        //}


        private string LoadText()
        {

            string textcontent = string.Empty;
            try
            {
                Assembly _assembly = Assembly.GetEntryAssembly();

                StreamReader _textStreamReader = null;
                try
                {
                    _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("Starcounter.InstallerWPF.resources.LicenseAgreement.html"));
                }
                catch
                {
                    _textStreamReader = new StreamReader(File.OpenRead(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "LicenseAgreement.html")));
                }

                string line = _textStreamReader.ReadToEnd();


                //Assembly _assembly = Assembly.GetEntryAssembly();

                //StreamReader _textStreamReader = null;
                //try
                //{
                //    _textStreamReader = new StreamReader(_assembly.GetManifestResourceStream("htmlToFlowDocument.license-agreement.html"));
                //}
                //catch
                //{
                //    _textStreamReader = new StreamReader(File.OpenRead(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "license-agreement.html")));
                //}

                //string line = _textStreamReader.ReadToEnd();


                textcontent = line;


                _textStreamReader.Close();

                //this.myBrowser.NavigateToString(line);

            }
            catch (Exception)
            {
                //System.Windows.Forms.MessageBox.Show("Error" + e.Message);
            }

            return textcontent;
        }


    }



}
