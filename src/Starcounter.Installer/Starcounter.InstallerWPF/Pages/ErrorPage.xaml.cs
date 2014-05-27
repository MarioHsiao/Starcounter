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
using System.Text.RegularExpressions;
using Starcounter.Internal;

namespace Starcounter.InstallerWPF.Pages
{
    /// <summary>
    /// Interaction logic for ErrorPage.xaml
    /// </summary>
    public partial class ErrorPage : BasePage
    {
        #region Properties

        public bool HasError
        {
            get
            {
                return this.Exception != null;
            }
        }

        private Exception _Exception;
        public Exception Exception
        {
            get
            {
                return _Exception;
            }
            set
            {
                if (_Exception == value) return;
                _Exception = value;
                this.OnPropertyChanged("Exception");
                this.OnPropertyChanged("Message");
            }
        }

        public FrameworkElement Message
        {
            get
            {
                if (this.HasError)
                {
                    if (this.Exception is InstallerEngine.InstallerAbortedException)
                    {
                        return this.ParseText(this.Exception.Message);
                        //return this.Exception.Message;
                    }

                    // Trying to extract an error message.
                    ErrorMessage errMessage;
                    Boolean isScException = ErrorCode.TryGetCodedMessage(this.Exception, out errMessage);
                    if (isScException && (!String.IsNullOrEmpty(errMessage.Body))) {
                        return this.ParseText(errMessage.Body);
                    }

                    return this.ParseText(this.Exception.ToString());
                    //return this.Exception.ToString();
                }

                return this.ParseText("Unknown Error");
                //return "Unknown Error";
            }
          
        }
        #endregion

        public ErrorPage()
        {
            InitializeComponent();
        }

        #region Message handling

        private FrameworkElement ParseText(string text)
        {
            FlowDocument document = new FlowDocument();

            Paragraph pg = new Paragraph();

            IList<Inline> inlines = this.TextToInlines(text);

            foreach (Inline inline in inlines)
            {
                pg.Inlines.Add(inline);
            }

            document.Blocks.Add(pg);

            // Create RichTextBox
            System.Windows.Controls.RichTextBox rtb = new System.Windows.Controls.RichTextBox();
            rtb.IsTabStop = false;

            rtb.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;
            rtb.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch;


            rtb.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            rtb.BorderThickness = new Thickness(0);
            rtb.IsReadOnly = true;
            rtb.IsDocumentEnabled = true;
            rtb.Document = document;


            System.Windows.Data.Binding foregroundBinding = new System.Windows.Data.Binding("Foreground");
            foregroundBinding.Source = this;
            rtb.SetBinding(ForegroundProperty, foregroundBinding);

            rtb.Resources.MergedDictionaries.Add(System.Windows.Application.Current.Resources);

            rtb.Background = Brushes.Transparent;
            rtb.Padding = new Thickness(0);
            rtb.Margin = new Thickness(0);
            rtb.VerticalAlignment = System.Windows.VerticalAlignment.Top;

            return rtb;
        }

        private IList<Inline> TextToInlines(string text)
        {
            IList<Inline> inlines = new List<Inline>();

            string sourcetext = text;

            string txt = string.Empty;

            while (sourcetext.Length > 0)
            {
                int startpos = sourcetext.IndexOf("http://", StringComparison.InvariantCultureIgnoreCase);
                if (startpos != -1)
                {

                    txt = sourcetext.Substring(0, startpos);
                    inlines.Add(new Run(txt));

                    int endpos = sourcetext.IndexOf(" ", startpos);
                    if (endpos == -1)
                    {
                        endpos = sourcetext.Length;
                    }

                    if (endpos != -1)
                    {
                        txt = sourcetext.Substring(startpos, endpos - startpos);


                        sourcetext = sourcetext.Substring(endpos);

                        string url = txt.Substring(0, txt.Length);

                        if (url.EndsWith("."))
                        {
                            url = url.Substring(0, url.Length - 1);
                        }

                        Hyperlink link = new Hyperlink(new Run(url));

                        link.Command = NavigationCommands.GoToPage;
                        link.CommandParameter = url;

                        Span span = new Span(link);

                        inlines.Add(span);
                    }
                    else
                    {
                        txt = sourcetext;
                        inlines.Add(new Run(txt));
                        sourcetext = string.Empty;

                    }
                }
                else
                {
                    txt = sourcetext;
                    inlines.Add(new Run(txt));
                    sourcetext = string.Empty;
                }
            }

            //// Add test link
            //Hyperlink link = new Hyperlink(new Run("http://www.dn.se"));
            //link.Command = NavigationCommands.GoToPage;
            //link.CommandParameter = "http://www.google.se";
            //inlines.Add(link);
            //inlines.Add(new Run("A bit more text content\r\n"));
            //inlines.Add(new Run("LAST\r\n"));


            return inlines;
        }

        protected string MakeLink(string txt)
        {

            return Regex.Replace(

                          txt,

                          @"(http|ftp|https):\/\/[\w\-_]+(\.[\w\-_]+)+([\w\-\.,@?^=%&amp;:/~\+#]*[\w\-\@?^=%&amp;/~\+#])",

                          delegate(Match match)
                          {

                              return string.Format("<a href=\"{0}\">{0}</a>", match.ToString());

                          });


        }

        #endregion
    }
}
