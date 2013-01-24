using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Diagnostics;

namespace Starcounter.InstallerWPF.DemoSequence
{
    public abstract class BaseDemoControl : Control, INotifyPropertyChanged
    {
        #region Properties

        virtual public string Image
        {
            get
            {
                throw new NotImplementedException("Image");
            }
        }
  
        private string _Title;
        public string Title
        {
            get
            {
                return this._Title;
            }
            set
            {
                if (string.Equals(this._Title, value)) return;
                this._Title = value;
                this.OnPropertyChanged("Title");
            }
        }

        private string _Description;
        public string Description
        {
            get
            {
                return this._Description;
            }
            set
            {
                if (string.Equals(this._Description, value)) return;
                this._Description = value;
                this.OnPropertyChanged("Description");
            }
        }

        private string _TemplateName;
        public string TemplateName
        {
            get
            {
                return this._TemplateName;
            }
            set
            {
                if (string.Equals(this._TemplateName, value)) return;
                this._TemplateName = value;
                this.OnPropertyChanged("TemplateName");
            }
        }

        private Process _ProjectProcess;
        public Process ProjectProcess
        {
            get
            {
                return this._ProjectProcess;
            }
            set
            {
                if (this._ProjectProcess == value) return;
                this._ProjectProcess = value;
                this.OnPropertyChanged("ProjectProcess");
            }
        }

        private Process _VisualStudioProcess;
        public Process VisualStudioProcess
        {
            get
            {
                return this._VisualStudioProcess;
            }
            set
            {
                if (this._VisualStudioProcess == value) return;
                this._VisualStudioProcess = value;
                this.OnPropertyChanged("VisualStudioProcess");
            }
        }

        #endregion

        #region Events

        #region Complete Event

        public delegate void CompleteEventHandler(object sender, CompletedEventArgs e);

        public event CompleteEventHandler Complete;
        protected virtual void OnComplete(CompletedEventArgs e)
        {
            if (Complete != null)
            {
                Complete(this, e);
            }

        }

        #endregion

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
}
