using Starcounter.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Starcounter.InstallerWPF.Converters
{
    public class HtmlToXamlConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string text = value as String;


            text = this.FixTag("html", text);
            text = this.FixTag("head", text);
            text = this.FixTag("title", text);
            text = this.FixTag("link", text);
            text = this.FixTag("body", text);
            text = this.FixTag("h1", text);
            text = this.FixTag("h2", text);
            text = this.FixTag("p", text);
            text = this.FixTag("strong", text);
            return text;

        }


        private string FixTag(string tag, string text)
        {

            switch (tag)
            {
                case "html":
                    return this.Fix_html(text);
                case "head":
                    return this.Fix_head(text);
                case "title":
                    return this.Fix_title(text);
                case "link":
                    return this.Fix_link(text);
                case "body":
                    return this.Fix_body(text);
                case "h1":
                    return this.Fix_h1(text);
                case "p":
                    return this.Fix_p(text);
                case "strong":
                    return this.Fix_strong(text);
                case "h2":
                    return this.Fix_h2(text);
                default:
                    break;

            }
            return text;
        }


        private string Fix_html(string text)
        {

            string flowdocStart = "<FlowDocument " + Environment.NewLine;
            flowdocStart += "xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"" + Environment.NewLine;
            flowdocStart += "xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\">" + Environment.NewLine;

            string flowdocStartStop = "</FlowDocument>" + Environment.NewLine;

            text = text.Replace("<html>", flowdocStart);
            text = text.Replace("</html>", flowdocStartStop);

            return text;
        }


        private string Fix_head(string text)
        {
            text = text.Replace("<head>", string.Empty);
            text = text.Replace("</head>", string.Empty);

            return text;
        }

        private string Fix_title(string text)
        {
            text = text.Replace("<title>", string.Empty);
            text = text.Replace("</title>", string.Empty);

            return text;
        }

        private string Fix_link(string text)
        {
            text = text.Replace("<link>", string.Empty);
            text = text.Replace("</link>", string.Empty);

            int startIndex = 0;

            while (true)
            {
                int pos = text.IndexOf("<link ", startIndex);
                if (pos != -1)
                {
                    int endPos = text.IndexOf("/>", startIndex);

                    text = text.Remove(pos, endPos - pos + 2);
                    startIndex = endPos;
                }
                else
                {
                    break;
                }
            }


            return text;
        }


        private string Fix_body(string text)
        {
            text = text.Replace("<body>", string.Empty);
            text = text.Replace("</body>", string.Empty);

            return text;
        }

        private string Fix_h1(string text)
        {
            //<Run FontWeight="bold">Markup</Run>

            text = text.Replace("<h1>", "<Paragraph><Run FontSize=\"28\" FontWeight=\"bold\">");
            text = text.Replace("</h1>", "</Run></Paragraph>");

            return text;
        }

        private string Fix_h2(string text)
        {
            text = text.Replace("<h2>", "<Paragraph><Run FontSize=\"23\" FontWeight=\"bold\">");
            text = text.Replace("</h2>", "</Run></Paragraph>");

            return text;
        }

        private string Fix_strong(string text)
        {
            text = text.Replace("<strong>", "<Run FontWeight=\"bold\">");
            text = text.Replace("</strong>", "</Run>");

            return text;
        }
        private string Fix_p(string text)
        {

            while (true)
            {
                if (text.IndexOf("<p>") == -1) break;

                text = text.Replace("<p>", "<Paragraph KeepTogether=\"True\">");
                text = text.Replace("</p>", "</Paragraph>");

            }

            while (true)
            {
                if (text.IndexOf("<p class=\"highlight\">") == -1) break;

                text = text.Replace("<p class=\"highlight\">", "<Paragraph>");
                text = text.Replace("</p>", "</Paragraph>");
            }

            return text;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw ErrorCode.ToException(Error.SCERRNOTIMPLEMENTED, "ConvertBack function.");
        }

        #endregion
    }
}
