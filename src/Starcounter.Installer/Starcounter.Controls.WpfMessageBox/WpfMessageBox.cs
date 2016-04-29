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
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Reflection;
using System.IO;

namespace Starcounter.Controls
{

    public class WpfMessageBox : Window, INotifyPropertyChanged
    {

        #region Win32 import

        private const uint SC_CLOSE = 0xF060;
        private const int MF_DISABLED = 0x00000002;
        private const int MF_ENABLED = 0x00000000;

        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_DLGMODALFRAME = 0x0001;
        private const int SWP_NOSIZE = 0x0001;
        private const int SWP_NOMOVE = 0x0002;
        private const int SWP_NOZORDER = 0x0004;
        private const int SWP_FRAMECHANGED = 0x0020;
        private const uint WM_SETICON = 0x0080;

        private const int ICON_SMALL = 0;
        private const int ICON_BIG = 1;

        private const int GWL_STYLE = -16;

        private const int WS_SYSMENU = 0x80000;

        private const uint MF_BYCOMMAND = 0x00000000;
        private const uint MF_GRAYED = 0x00000001;

        private const int WM_SHOWWINDOW = 0x00000018;
        private const int WM_CLOSE = 0x10;

        private const int SC_MAXIMIZE = 0xF030;

        private const int SC_MINIMIZE = 0xF020;
        private const int SC_RESTORE = 0xF120;
        private const int SC_SIZE = 0xF000;


        [DllImport("user32.dll")]
        private static extern bool EnableMenuItem(IntPtr hMenu, uint uIDEnableItem, uint uEnable);

        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

        [DllImport("User32.Dll")]
        public static extern IntPtr DrawMenuBar(IntPtr hwnd);


        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hwnd, IntPtr hwndInsertAfter, int x, int y, int width, int height, uint flags);

        [DllImport("user32.dll")]
        static extern IntPtr SendMessage(IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        static extern bool RemoveMenu(IntPtr hMenu, uint uPosition, uint uFlags);

        #endregion

        #region Properties

        FrameworkElement _Document;
        public FrameworkElement Document
        {
            get
            {


                return _Document;
            }

            set
            {
                if (this._Document == value) return;
                this._Document = value;
                this.OnPropertyChanged("Document");
            }

        }

        string _MessageBoxText;
        public string MessageBoxText
        {
            get
            {
                return _MessageBoxText;
            }

            set
            {
                if (string.Equals(this._MessageBoxText, value)) return;
                this._MessageBoxText = value;

                this.Document = this.ParseText(this._MessageBoxText);


                this.OnPropertyChanged("MessageBoxText");
            }

        }

        string _Caption;
        public string Caption
        {
            get
            {
                return _Caption;
            }

            set
            {
                if (string.Equals(this._Caption, value)) return;
                this._Caption = value;
                this.OnPropertyChanged("Caption");
            }

        }

        WpfMessageBoxButton _Button = WpfMessageBoxButton.OK;
        public WpfMessageBoxButton Button
        {
            get
            {
                return _Button;
            }

            set
            {

                if (this._Button == value) return;
                this._Button = value;

                this.CloseSystemMenuButtonIsEnabled = value != WpfMessageBoxButton.YesNo;


                this.OnPropertyChanged("Button");
            }

        }

        WpfMessageBoxImage _Icon = WpfMessageBoxImage.None;
        public new WpfMessageBoxImage Icon
        {
            get
            {

                return _Icon;
            }

            protected set
            {
                if (this._Icon == value) return;
                this._Icon = value;

                this.Image = this.SetIcon(value);

                if (this.Image_Part != null)
                {
                    if (this._Icon == WpfMessageBoxImage.None)
                    {
                        this.Image_Part.Visibility = System.Windows.Visibility.Collapsed;
                    }
                    else
                    {
                        this.Image_Part.Visibility = System.Windows.Visibility.Visible;
                    }
                }

                this.OnPropertyChanged("Icon");
            }
        }

        ImageSource _Image;
        public ImageSource Image
        {
            get
            {
                return _Image;
            }

            protected set
            {
                if (this._Image == value) return;
                this._Image = value;
                this.OnPropertyChanged("Image");
            }

        }

        WpfMessageBoxResult _DefaultResult = WpfMessageBoxResult.None;
        public WpfMessageBoxResult DefaultResult
        {
            get
            {
                return _DefaultResult;
            }

            set
            {
                if (this._DefaultResult == value) return;
                this._DefaultResult = value;
                this.OnPropertyChanged("DefaultResult");
            }

        }

        WpfMessageBoxResult _MessageBoxResult = WpfMessageBoxResult.None;
        public WpfMessageBoxResult MessageBoxResult
        {
            get
            {
                return _MessageBoxResult;
            }

            set
            {
                if (this._MessageBoxResult == value) return;
                this._MessageBoxResult = value;
                this.OnPropertyChanged("MessageBoxResult");
            }

        }


        private bool _CloseSystemMenuButtonIsEnabled = true;
        private bool CloseSystemMenuButtonIsEnabled
        {
            get
            {
                return this._CloseSystemMenuButtonIsEnabled;
            }
            set
            {
                if (this._CloseSystemMenuButtonIsEnabled == value) return;
                this._CloseSystemMenuButtonIsEnabled = value;



                var hwnd = new WindowInteropHelper(this).Handle;
                IntPtr menu = GetSystemMenu(hwnd, false);

                if (value)
                {
                    EnableMenuItem(menu, SC_CLOSE, MF_ENABLED);
                }
                else
                {
                    EnableMenuItem(menu, SC_CLOSE, MF_DISABLED);
                }

                // Redraw
                DrawMenuBar(hwnd);

                this.OnPropertyChanged("CloseSystemMenuButtonIsEnabled");
            }
        }


        #endregion

        #region Commands

        #region Ok

        public static readonly RoutedUICommand Command_Ok = new RoutedUICommand();

        private void CanExecute_Ok_Command(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = this.Button == WpfMessageBoxButton.OK || this.Button == WpfMessageBoxButton.OKCancel;
        }
        private void Executed_OK_Command(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            this.MessageBoxResult = WpfMessageBoxResult.OK;
            this.CloseSystemMenuButtonIsEnabled = true;
            this.Close();
        }

        #endregion

        #region Cancel

        public static readonly RoutedUICommand Command_Cancel = new RoutedUICommand();

        private void CanExecute_Cancel_Command(object sender, CanExecuteRoutedEventArgs e)
        {

            e.Handled = true;
            e.CanExecute = this.Button == WpfMessageBoxButton.OKCancel || this.Button == WpfMessageBoxButton.YesNoCancel;
        }
        private void Executed_Cancel_Command(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            this.MessageBoxResult = WpfMessageBoxResult.Cancel;
            this.CloseSystemMenuButtonIsEnabled = true;
            this.Close();
        }

        #endregion

        #region Yes

        public static readonly RoutedUICommand Command_Yes = new RoutedUICommand();

        private void CanExecute_Yes_Command(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = this.Button == WpfMessageBoxButton.YesNo || this.Button == WpfMessageBoxButton.YesNoCancel;
        }
        private void Executed_Yes_Command(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            this.MessageBoxResult = WpfMessageBoxResult.Yes;
            this.CloseSystemMenuButtonIsEnabled = true;
            this.Close();
        }

        #endregion

        #region No

        public static readonly RoutedUICommand Command_No = new RoutedUICommand();

        private void CanExecute_No_Command(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;
            e.CanExecute = this.Button == WpfMessageBoxButton.YesNo || this.Button == WpfMessageBoxButton.YesNoCancel;
        }
        private void Executed_No_Command(object sender, ExecutedRoutedEventArgs e)
        {
            e.Handled = true;
            this.MessageBoxResult = WpfMessageBoxResult.No;
            this.CloseSystemMenuButtonIsEnabled = true;

            this.Close();
        }

        #endregion

        #region GoToPage


        private void CanExecute_GoToPage_Command(object sender, CanExecuteRoutedEventArgs e)
        {
            e.Handled = true;

            if (string.IsNullOrEmpty(e.Parameter as string))
            {
                e.CanExecute = false;
            }
            else
            {
                e.CanExecute = true;
            }

        }

        private void Executed_GoToPage_Command(object sender, ExecutedRoutedEventArgs e)
        {



            // Used to go to web page
            if (e.Handled == false && !string.IsNullOrEmpty(e.Parameter as string))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(e.Parameter as string));
                    e.Handled = true;
                }
                catch (Win32Exception ee)
                {
                    string message = "Can not open external browser." + Environment.NewLine + ee.Message + Environment.NewLine + e.Parameter;
                    //this.OnError(new Exception(message));
                }
            }

        }


        #endregion

        #endregion

        static WpfMessageBox()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(WpfMessageBox), new FrameworkPropertyMetadata(typeof(WpfMessageBox)));
        }

        private WpfMessageBox()
        {
            // Command bindings
            CommandBinding CommandBinding_Ok = new CommandBinding(WpfMessageBox.Command_Ok, this.Executed_OK_Command, this.CanExecute_Ok_Command);
            this.CommandBindings.Add(CommandBinding_Ok);

            CommandBinding CommandBinding_Cancel = new CommandBinding(WpfMessageBox.Command_Cancel, this.Executed_Cancel_Command, this.CanExecute_Cancel_Command);
            this.CommandBindings.Add(CommandBinding_Cancel);

            CommandBinding CommandBinding_Yes = new CommandBinding(WpfMessageBox.Command_Yes, this.Executed_Yes_Command, this.CanExecute_Yes_Command);
            this.CommandBindings.Add(CommandBinding_Yes);

            CommandBinding CommandBinding_No = new CommandBinding(WpfMessageBox.Command_No, this.Executed_No_Command, this.CanExecute_No_Command);
            this.CommandBindings.Add(CommandBinding_No);

            CommandBinding CommandBinding_GoToPage = new CommandBinding(NavigationCommands.GoToPage, this.Executed_GoToPage_Command, this.CanExecute_GoToPage_Command);
            this.CommandBindings.Add(CommandBinding_GoToPage);

            this.ResizeMode = System.Windows.ResizeMode.NoResize;

            this.Loaded += new RoutedEventHandler(WpfMessageBox_Loaded);
            this.SourceInitialized += new EventHandler(WpfMessageBox_SourceInitialized);
        }

        void WpfMessageBox_SourceInitialized(object sender, EventArgs e)
        {
            HwndSource hwndSource = PresentationSource.FromVisual(this) as HwndSource;

            if (hwndSource != null)
            {
                hwndSource.AddHook(HwndSourceHook);
            }

            //this.CloseSystemMenuButtonIsEnabled = this.Button != WpfMessageBoxButton.YesNo;

            //this.ResizeMode = System.Windows.ResizeMode.NoResize;

            // Get this window's handle
            IntPtr hwnd = new WindowInteropHelper(this).Handle;

            // Update the window's non-client area to reflect the changes
            SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE | SWP_NOZORDER | SWP_FRAMECHANGED);


            // Change the extended window style to not show a window icon
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);


            // reset the icon, both calls important
            SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_SMALL, IntPtr.Zero);
            SendMessage(hwnd, WM_SETICON, (IntPtr)ICON_BIG, IntPtr.Zero);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_DLGMODALFRAME);


            // Hide system menu
            //SetWindowLong(hwnd, GWL_STYLE, GetWindowLong(hwnd, GWL_STYLE) & ~WS_SYSMENU);


            // Remove unwanted menu items
            IntPtr hMenu = GetSystemMenu(hwnd, false);

            RemoveMenu(hMenu, SC_MAXIMIZE, MF_BYCOMMAND);
            RemoveMenu(hMenu, SC_MINIMIZE, MF_BYCOMMAND);
            RemoveMenu(hMenu, SC_RESTORE, MF_BYCOMMAND);
            RemoveMenu(hMenu, SC_SIZE, MF_BYCOMMAND);

            if (this.Button == WpfMessageBoxButton.YesNo)
            {
                RemoveMenu(hMenu, SC_CLOSE, MF_BYCOMMAND);
            }

            //RemoveMenu(hMenu, (uint)(n - 1), MF_BYPOSITION | MF_REMOVE);

            // http://msdn.microsoft.com/en-us/library/ms646360(v=vs.85).aspx

            //EnableMenuItem(hMenu, SC_MAXIMIZE, MF_BYCOMMAND | MF_GRAYED);
            //EnableMenuItem(hMenu, SC_MINIMIZE, MF_BYCOMMAND | MF_GRAYED);
            //EnableMenuItem(hMenu, SC_RESTORE, MF_BYCOMMAND | MF_GRAYED);
            //EnableMenuItem(hMenu, SC_SIZE, MF_BYCOMMAND | MF_GRAYED);
        }

        void WpfMessageBox_Loaded(object sender, RoutedEventArgs e)
        {
            this.ResizeMode = System.Windows.ResizeMode.NoResize;

            Size minSize = new Size();
            // Set size

            // Button width
            if (this.ButtonBar_Part != null)
            {
                minSize.Width = Math.Max(minSize.Width, this.ButtonBar_Part.ActualWidth + this.ButtonBar_Part.Margin.Left + this.ButtonBar_Part.Margin.Right);
                //minSize.Height = Math.Max(minSize.Height, this.ButtonBar_Part.ActualHeight);
            }

            // Title width
            Size textSize = this.GetTextSize(this.Title);
            minSize.Width = Math.Max(minSize.Width, textSize.Width);
            //minSize.Height = Math.Max(minSize.Height, textSize.Height);

            // image
            double imageWidth = 0;
            if (this.Image_Part != null && this.Image_Part.Visibility == System.Windows.Visibility.Visible)
            {
                imageWidth = this.Image_Part.ActualWidth + this.Image_Part.Margin.Left + this.Image_Part.Margin.Right;
            }

            // Content width
            Size messageSize = this.GetTextSize(this.MessageBoxText);
            messageSize.Width += this.MainContent_Part.Margin.Left + this.MainContent_Part.Margin.Right;

            messageSize.Width += 50;

            minSize.Width = Math.Max(minSize.Width, messageSize.Width + imageWidth);
            //minSize.Width = Math.Max(minSize.Width, this.MainContent_Part.ActualWidth + imageWidth + this.MainContent_Part.Margin.Left + this.MainContent_Part.Margin.Right);


            Size maxSize = this.GetMaxSize();

            if (this.MainContent_Part.ActualHeight > maxSize.Height)
            {
                // try to max the window wider, the some text may not wrap and we can gain some content height
                maxSize.Width = maxSize.Width;
            }
            else
            {
                maxSize.Width = 480;
            }


            //this.Height = maxSize.Height;
            this.MainContent_Part.Width = Math.Min(maxSize.Width, minSize.Width);
            this.MainContent_Part.MaxHeight = maxSize.Height;
            //this.Content_Part.MaxWidth = Math.Min(maxSize.Width, minSize.Width);
            //this.Content_Part.MinWidth = Math.Min(maxSize.Width, minSize.Width);

            // Content width


        }

        private IntPtr HwndSourceHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                //case WM_SHOWWINDOW:
                //    {
                //        IntPtr hMenu = GetSystemMenu(hwnd, false);
                //        if (hMenu != IntPtr.Zero)
                //        {
                //            EnableMenuItem(hMenu, SC_CLOSE, MF_BYCOMMAND | MF_GRAYED);
                //        }
                //    }
                //    break;
                case WM_CLOSE:
                    if (!CloseSystemMenuButtonIsEnabled)
                    {
                        handled = true;
                    }

                    if (this.MessageBoxResult == WpfMessageBoxResult.None)
                    {

                        if (this.Button == WpfMessageBoxButton.OK)
                        {
                            this.MessageBoxResult = WpfMessageBoxResult.OK;
                        }
                        else if (this.Button == WpfMessageBoxButton.OKCancel || this.Button == WpfMessageBoxButton.YesNoCancel)
                        {
                            this.MessageBoxResult = WpfMessageBoxResult.Cancel;
                        }
                        else if (this.Button == WpfMessageBoxButton.YesNo)
                        {
                            this.MessageBoxResult = WpfMessageBoxResult.No;
                        }

                    }



                    break;
            }
            return IntPtr.Zero;
        }

        System.Windows.Controls.Panel ButtonBar_Part;
        Image Image_Part;
        FrameworkElement MainContent_Part;

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            this.ButtonBar_Part = this.GetTemplateChild("PART_Buttons") as System.Windows.Controls.Panel;
            this.Image_Part = this.GetTemplateChild("PART_Image") as Image;
            this.MainContent_Part = this.GetTemplateChild("PART_MainContent") as FrameworkElement;


            if (this.Image_Part != null)
            {
                if (this._Icon == WpfMessageBoxImage.None)
                {
                    this.Image_Part.Visibility = System.Windows.Visibility.Collapsed;
                }
                else
                {
                    this.Image_Part.Visibility = System.Windows.Visibility.Visible;
                }
            }
        }

        public static WpfMessageBoxResult Show(string messageBoxText)
        {
            return WpfMessageBox.Show(messageBoxText, string.Empty);
        }

        public static WpfMessageBoxResult Show(string messageBoxText, string caption)
        {
            return WpfMessageBox.Show(messageBoxText, caption, WpfMessageBoxButton.OK);
        }

        public static WpfMessageBoxResult Show(string messageBoxText, string caption, WpfMessageBoxButton button)
        {
            return WpfMessageBox.Show(messageBoxText, caption, button, WpfMessageBoxImage.None);
        }

        public static WpfMessageBoxResult Show(string messageBoxText, string caption, WpfMessageBoxButton button, WpfMessageBoxImage icon)
        {
            WpfMessageBoxResult defaultResult;

            if (button == WpfMessageBoxButton.OK || button == WpfMessageBoxButton.OKCancel)
            {
                defaultResult = WpfMessageBoxResult.OK;
            }
            else
            {
                defaultResult = WpfMessageBoxResult.Yes;
            }

            return WpfMessageBox.Show(messageBoxText, caption, button, icon, defaultResult);

        }

        public static WpfMessageBoxResult Show(string messageBoxText, string caption, WpfMessageBoxButton button, WpfMessageBoxImage icon, WpfMessageBoxResult defaultResult)
        {
            WpfMessageBox win = new WpfMessageBox();

            // Fix so the window doesn't own itself
            if (System.Windows.Application.Current.MainWindow != win) {
                win.Owner = System.Windows.Application.Current.MainWindow;
            }

            win.MessageBoxText = messageBoxText;
            win.Caption = caption;
            win.Button = button;
            win.ShowInTaskbar = false;
            win.Topmost = true;
            win.Icon = icon;

            win.SizeToContent = SizeToContent.WidthAndHeight;
            win.DefaultResult = defaultResult;
            win.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            win.ShowDialog();

            return win.MessageBoxResult;
        }

        private Size GetMaxSize()
        {
            Size size = new Size();
            // Get screen size
            PresentationSource source = PresentationSource.FromVisual(System.Windows.Application.Current.MainWindow);
            Matrix m = source.CompositionTarget.TransformToDevice;

            WindowInteropHelper windowInteropHelper = new WindowInteropHelper(System.Windows.Application.Current.MainWindow);
            Screen screen = System.Windows.Forms.Screen.FromHandle(windowInteropHelper.Handle);

            double ScreenHeight = screen.Bounds.Height * m.M11;
            double ScreenWidth = screen.Bounds.Width * m.M22;

            size.Width = ScreenWidth * 0.6;
            size.Height = ScreenHeight * 0.9;

            return size;
        }

        private Size GetTextSize(string text)
        {
            Typeface typeface;
            double fontSize;
            Size size = new Size();

            if( string.IsNullOrEmpty( text ) ){
                return size;
            }

            PresentationSource source = PresentationSource.FromVisual(System.Windows.Application.Current.MainWindow);
            Matrix m = source.CompositionTarget.TransformToDevice;

            WindowInteropHelper windowInteropHelper = new WindowInteropHelper(System.Windows.Application.Current.MainWindow);
            Screen screen = System.Windows.Forms.Screen.FromHandle(windowInteropHelper.Handle);


            FontFamily fontFamily = this.GetValue(TextElement.FontFamilyProperty) as FontFamily;
            if (fontFamily == null)
            {
                typeface = new Typeface("Segoe UI");
            }
            else
            {
                typeface = new Typeface(fontFamily.Source);
            }

            fontSize = (double)this.GetValue(TextElement.FontSizeProperty);

            FormattedText ft = new FormattedText(text, CultureInfo.CurrentCulture, System.Windows.FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);

            size.Width = ft.Width * m.M22;
            size.Height = ft.Height * m.M11;
            return size;
        }

        /// <summary>
        /// Sets the icon.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <returns></returns>
        private ImageSource SetIcon(WpfMessageBoxImage image)
        {
            System.Drawing.Icon icon = System.Drawing.SystemIcons.Application;
            string fileName = string.Empty;

            if (image == WpfMessageBoxImage.Asterisk)
            {
                fileName = "StatusAnnotations_Information_32xLG_color.png";
                icon = System.Drawing.SystemIcons.Asterisk;
            }
            else if (image == WpfMessageBoxImage.Error)
            {
                fileName = "StatusAnnotations_Critical_32xLG_color.png";
                icon = System.Drawing.SystemIcons.Error;
            }
            else if (image == WpfMessageBoxImage.Exclamation)
            {
                fileName = "StatusAnnotations_Warning_32xLG_color.png";
                icon = System.Drawing.SystemIcons.Exclamation;
            }
            else if (image == WpfMessageBoxImage.Hand)
            {
                fileName = "StatusAnnotations_Critical_32xLG_color.png";
                icon = System.Drawing.SystemIcons.Hand;
            }
            else if (image == WpfMessageBoxImage.Information)
            {
                fileName = "StatusAnnotations_Information_32xLG_color.png";
                icon = System.Drawing.SystemIcons.Information;
            }
            else if (image == WpfMessageBoxImage.None)
            {
                return null;
            }
            else if (image == WpfMessageBoxImage.Question)
            {
                fileName = "StatusAnnotations_Help_and_inconclusive_32xLG_color.png";
                icon = System.Drawing.SystemIcons.Question;
            }
            else if (image == WpfMessageBoxImage.Stop)
            {
                fileName = "StatusAnnotations_Critical_32xLG_color.png";
                icon = System.Drawing.SystemIcons.Error;
            }
            else if (image == WpfMessageBoxImage.Warning)
            {
                fileName = "StatusAnnotations_Warning_32xLG_color.png";
                icon = System.Drawing.SystemIcons.Warning;
            }
            else if (image == WpfMessageBoxImage.Ok) {
                fileName = "StatusAnnotations_Complete_and_ok_32xLG_color.png";
                icon = System.Drawing.SystemIcons.Information;
            }

            if( string.IsNullOrEmpty( fileName )) {
                return null;
            }

            Assembly _assembly = this.GetType().Assembly;
            Assembly myAssembly = Assembly.GetExecutingAssembly();
            Stream s = myAssembly.GetManifestResourceStream(string.Format("Starcounter.Controls.images.{0}",fileName));

            BitmapImage bs = new BitmapImage();
            bs.BeginInit(); 
            bs.StreamSource = s;
            bs.EndInit();
     
            //BitmapSource bs = System.Windows.Interop.Imaging.CreateBitmapSourceFromHIcon(icon.Handle, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            return bs;

        }

        #region Message handling

        private FrameworkElement ParseText(string text)
        {
            FlowDocument document = new FlowDocument();


            // <RichTextBox>
            //<FlowDocument>
            //                                         <Paragraph>
            //                                             A RichTextBox with
            //                                             <Bold>initial content</Bold> in it.
            //                                             <Hyperlink Command="NavigationCommands.GoToPage" CommandParameter="http://www.starcounter.com/wiki/Database_Files#Databases"  >Live Games</Hyperlink>
            //                                         </Paragraph>
            //                                     </FlowDocument>
            // </RichTextBox>

            //Span spanx = new Span();
            //spanx.Inlines.Add(new Run("A bit of text content..."));
            //spanx.Inlines.Add(new Run("A bit more text content..."));


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

            rtb.HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch;       // *NEW*
            rtb.HorizontalContentAlignment = System.Windows.HorizontalAlignment.Stretch;// *NEW*


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


            // Shadow object ( to get the width and height )
            //ScrollViewer sv = new ScrollViewer();
            //sv.Padding = new Thickness(5, 20, 15, 20); // TODO:!!
            //sv.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
            //TextBlock tb = new TextBlock();

            //tb.FontFamily = rtb.FontFamily;
            //tb.FontSize = rtb.FontSize;
            //tb.FontStretch = rtb.FontStretch;
            //tb.FontStyle = rtb.FontStyle;
            //tb.FontWeight = rtb.FontWeight;

            //tb.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
            //tb.VerticalAlignment = System.Windows.VerticalAlignment.Top;
            //tb.TextWrapping = TextWrapping.Wrap;
            //tb.Margin = new Thickness(0, 0, 0, 0); // RichTextBox has this hardcoded
            //IList<Inline> inlines2 = this.TextToInlines(text);
            //foreach (Inline inline in inlines2)
            //{
            //    tb.Inlines.Add(inline);
            //}
            ////sv.Content = tb;
            //this.ShadowElement = sv;

            return rtb;
        }

        private IList<Inline> TextToInlines(string text)
        {
            IList<Inline> inlines = new List<Inline>();

            //inlines.Add(new Run(text));

            string sourcetext = text;

            string txt = string.Empty;

            while (sourcetext.Length > 0)
            {
                int startpos = sourcetext.IndexOf("http://", StringComparison.InvariantCultureIgnoreCase);
                if (startpos < 0)
                    startpos = sourcetext.IndexOf("https://", StringComparison.InvariantCultureIgnoreCase);

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


            //return "oj";


            //Regex regx = new Regex("http(s)?://([\\w+?\\.\\w+])+([a-zA-Z0-9\\~\\!\\@\\#\\$\\%\\^\\&amp;\\*\\(\\)_\\-\\=\\+\\\\\\/\\?\\.\\:\\;\\'\\,]*)?", RegexOptions.IgnoreCase);

            //MatchCollection mactches = regx.Matches(txt);

            //foreach (Match match in mactches)
            //{
            //    txt = txt.Replace(match.Value, "<a href='" + match.Value + "'>" + match.Value + "</a>");
            //}

            //return txt;
        }

        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string fieldName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(fieldName));
            }
        }

        #endregion

    }

    // Summary:
    //     Specifies the buttons that are displayed on a message box. Used as an argument
    //     of the Overload:System.Windows.MessageBox.Show method.
    public enum WpfMessageBoxButton
    {
        // Summary:
        //     The message box displays an OK button.
        OK = 0,
        //
        // Summary:
        //     The message box displays OK and Cancel buttons.
        OKCancel = 1,
        //
        // Summary:
        //     The message box displays Yes, No, and Cancel buttons.
        YesNoCancel = 3,
        //
        // Summary:
        //     The message box displays Yes and No buttons.
        YesNo = 4,
    }


    // Summary:
    //     Specifies which message box button that a user clicks. System.Windows.MessageBoxResult
    //     is returned by the Overload:System.Windows.MessageBox.Show method.
    public enum WpfMessageBoxResult
    {
        // Summary:
        //     The message box returns no result.
        None = 0,
        //
        // Summary:
        //     The result value of the message box is OK.
        OK = 1,
        //
        // Summary:
        //     The result value of the message box is Cancel.
        Cancel = 2,
        //
        // Summary:
        //     The result value of the message box is Yes.
        Yes = 6,
        //
        // Summary:
        //     The result value of the message box is No.
        No = 7,
    }


    // Summary:
    //     Specifies the icon that is displayed by a message box.
    public enum WpfMessageBoxImage
    {
        // Summary:
        //     No icon is displayed.
        None = 0,
        //
        // Summary:
        //     The message box displays an error icon.
        Error = 16,
        //
        // Summary:
        //     The message box displays a hand icon.
        Hand = 16,
        //
        // Summary:
        //     The message box displays a stop icon.
        Stop = 16,
        //
        // Summary:
        //     The message box displays a question mark icon.
        Question = 32,
        //
        // Summary:
        //     The message box displays an exclamation mark icon.
        Exclamation = 48,
        //
        // Summary:
        //     The message box displays a warning icon.
        Warning = 48,
        //
        // Summary:
        //     The message box displays an information icon.
        Information = 64,
        //
        // Summary:
        //     The message box displays an asterisk icon.
        Asterisk = 64,
        //
        // Summary:
        //     The message box displays an ok icon.
        Ok = 128
    }

    public class IsDefaultConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            WpfMessageBoxResult defaultButton = (WpfMessageBoxResult)value;

            if (parameter == null) return false;

            string buttonId = parameter.ToString();


            if (buttonId.Equals("OK"))
            {
                if (defaultButton == WpfMessageBoxResult.OK)
                {
                    return true;
                }
            }
            else if (buttonId.Equals("YES"))
            {
                if (defaultButton == WpfMessageBoxResult.Yes)
                {
                    return true;
                }
            }
            else if (buttonId.Equals("NO"))
            {
                if (defaultButton == WpfMessageBoxResult.No)
                {
                    return true;
                }
            }
            else if (buttonId.Equals("CANCEL"))
            {
                if (defaultButton == WpfMessageBoxResult.Cancel)
                {
                    return true;
                }
            }

            return false;

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    public class IsCancelConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            WpfMessageBoxButton buttons = (WpfMessageBoxButton)value;

            if (parameter == null) return false;

            string buttonId = parameter.ToString();


            if (buttonId.Equals("OK"))
            {
                if (buttons == WpfMessageBoxButton.OK)
                {
                    return true;
                }
            }
            else if (buttonId.Equals("YES"))
            {
                return false;
            }
            else if (buttonId.Equals("NO"))
            {
                if (buttons == WpfMessageBoxButton.YesNo)
                {
                    return true;
                }

                return false;
            }
            else if (buttonId.Equals("CANCEL"))
            {
                if (buttons == WpfMessageBoxButton.OKCancel || buttons == WpfMessageBoxButton.YesNoCancel)
                {
                    return true;
                }
            }

            return false;

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

}
