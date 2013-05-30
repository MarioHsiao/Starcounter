using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.ComponentModel;
using System.Windows;
using Starcounter.InstallerWPF.Rules;
using System.Windows.Data;

namespace Starcounter.InstallerWPF.Pages {
    abstract public class BasePage : ContentControl, INotifyPropertyChanged {
        #region Properties

        private String _DisplayName;
        virtual public String DisplayName {
            get {
                return _DisplayName;
            }
            set {
                if (string.Equals(_DisplayName, value)) return;
                _DisplayName = value;
                this.OnPropertyChanged("DisplayName");
            }
        }

        private bool _HasProgress;
        virtual public bool HasProgress {
            get {
                return _HasProgress;
            }
            set {
                if (this._HasProgress == value) return;
                this._HasProgress = value;
                this.OnPropertyChanged("HasProgress");
            }
        }


        //private FrameworkElement _DisplayItem;
        //virtual public FrameworkElement DisplayItem
        //{
        //    get
        //    {
        //        //if (this._DisplayItem == null) return this.DisplayName;

        //        return _DisplayItem;
        //    }
        //    set
        //    {
        //        if( this._DisplayItem == value) return;
        //        _DisplayItem = value;
        //        this.OnPropertyChanged("DisplayItem");
        //    }
        //}


        virtual public bool CanGoNext {
            get {
                return !HasErrors;
            }
        }

        virtual public bool CanGoBack {
            get {
                return true;
            }
        }

        private bool _CanClose = true;
        virtual public bool CanClose {
            get {
                return _CanClose;
            }
            set {
                if (_CanClose == value) return;
                _CanClose = value;
                this.OnPropertyChanged("CanClose");
            }
        }

        private bool _HasErrors = false;
        virtual public bool HasErrors {
            get {
                return _HasErrors;
            }
            protected set {
                if (this._HasErrors == value) return;
                this._HasErrors = value;
                this.OnPropertyChanged("HasErrors");
                this.OnPropertyChanged("CanGoNext");
                this.OnPropertyChanged("CanGoBack");
            }
        }


        #region ProgressBar Properties

        private int _Progress = 0;
        public int Progress {
            get {
                return this._Progress;
            }
            set {
                if (this._Progress == value) return;

                this._Progress = value;
                this.OnPropertyChanged("Progress");
            }
        }

        private string _ProgressText;
        public string ProgressText {
            get {
                return this._ProgressText;
            }
            set {
                if (string.Equals(this._ProgressText, value)) return;

                this._ProgressText = value;
                this.OnPropertyChanged("ProgressText");
            }
        }

        #endregion

        #endregion

        #region Events

        public delegate void EventHandler(object sender, EventArgs e);

        #region Selected Event

        public event EventHandler Selected;
        protected virtual void OnSelected(EventArgs e) {
            if (Selected != null) {
                Selected(this, e);
            }
        }

        #endregion


        #region Deselected Event

        public event EventHandler Deselected;
        protected virtual void OnDeselected(EventArgs e) {
            if (Deselected != null) {
                Deselected(this, e);
            }
        }

        #endregion

        #endregion

        virtual public void OnSelected() {
            this.OnSelected(new EventArgs());
        }

        virtual public void OnDeselected() {
            this.OnDeselected(new EventArgs());
        }

        public BasePage()
            : base() {
            Focusable = false;
        }

        #region Error Handling

        int _errorCount = 0;
        protected void Validation_OnError(object sender, ValidationErrorEventArgs e) {

            if (e.Error == null) return;
            if (e.Error.ErrorContent == null) return;

            if (e.Error.ErrorContent is ErrorObject && ((ErrorObject)e.Error.ErrorContent).IsError == false) {
                return;
            }

            switch (e.Action) {
                case ValidationErrorEventAction.Added: {
                        _errorCount++;
                        break;
                    }
                case ValidationErrorEventAction.Removed: {
                        _errorCount--;
                        break;
                    }
            }

            this.HasErrors = _errorCount != 0;
        }

        protected void IsLoadedEvent(object sender, RoutedEventArgs e) {
            this.UpdateErrorBindings(sender as FrameworkElement);
        }

        protected void IsVisibleChangedEvent(object sender, DependencyPropertyChangedEventArgs e) {
            this.UpdateErrorBindings(sender as FrameworkElement);
        }

        // http://social.msdn.microsoft.com/Forums/en-US/wpf/thread/060e90f2-fc76-4405-bd83-eed9b7018106/
        private void UpdateErrorBindings(FrameworkElement element) {
            if (element != null && element.IsLoaded) {
                BindingExpression bindingExpression = null;
                if (element is TextBox) {
                    bindingExpression = element.GetBindingExpression(TextBox.TextProperty);
                }
                else if (element is ComboBox) {
                    bindingExpression = element.GetBindingExpression( ComboBox.TextProperty);
                }

                if (bindingExpression != null && bindingExpression.Status == BindingStatus.Active) {
                    bindingExpression.UpdateSource();
                    if (element.IsVisible == false) {
                        if (bindingExpression.HasError) {
                            Validation.ClearInvalid(bindingExpression);
                        }
                    }
                }
                else {
                }
            }
        }


        #endregion

        #region INotifyPropertyChanged Members

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string fieldName) {
            if (PropertyChanged != null) {
                PropertyChanged(this, new PropertyChangedEventArgs(fieldName));
            }
        }
        #endregion

    }
}
