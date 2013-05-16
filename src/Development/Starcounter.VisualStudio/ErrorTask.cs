
using Microsoft.VisualStudio.Shell;
using Starcounter.Internal;
using System;
using System.Diagnostics;

namespace Starcounter.VisualStudio {
    /// <summary>
    /// Represents an error task in the Error List window, originated
    /// from Starcounter extension code (e.g. deployment, debugging).
    /// </summary>
    internal class StarcounterErrorTask : ErrorTask {
        /// <summary>
        /// Possible help link.
        /// </summary>
        public string Helplink { get; set; }

        /// <summary>
        /// Gets a possible <see cref="ErrorMessage"/>. Tasks based
        /// on error messages can materialize themselves in the
        /// <see cref="ShowInUserMessageWindow"/> control when navigated
        /// to.
        /// </summary>
        public ErrorMessage ErrorMessage { get; private set; }

        /// <summary>
        /// Gets the <see cref="VsPackage"/> to which this task
        /// belong.
        /// </summary>
        public VsPackage Package {
            get;
            set;
        }

        /// <summary>
        /// Gets the <see cref="ErrorTaskSource"/> of this task.
        /// </summary>
        public ErrorTaskSource Source {
            get {
                if (this.SubcategoryIndex == (int)ErrorTaskSource.Debug)
                    return ErrorTaskSource.Debug;
                else
                    return ErrorTaskSource.Other;
            }
        }

        public StarcounterErrorTask()
            : base() {
        }

        public StarcounterErrorTask(Exception e)
            : base(e) {
            ErrorMessage message;

            try {
                if (ErrorCode.TryGetCodedMessage(e, out message)) {
                    BindToErrorMessage(message);
                }
            } catch { }
        }

        public StarcounterErrorTask(ErrorMessage message) {
            BindToErrorMessage(message);
        }

        public void ShowInUserMessageWindow() {
            throw new NotImplementedException();
        }

        public void ShowInBrowser() {
            Process.Start(new ProcessStartInfo(GetHelplinkOrDefault()) {
                UseShellExecute = true,
                ErrorDialog = true
            });
        }

        protected override void OnNavigate(EventArgs e) {
            if (this.ErrorMessage != null) {
                try {
                    ShowInUserMessageWindow();
                } catch {
                    ShowInBrowser();
                }
            }
            base.OnNavigate(e);
        }

        protected override void OnHelp(EventArgs e) {
            ShowInBrowser();
            base.OnHelp(e);
        }

        /// <summary>
        /// Gets the value of the <see cref="Helplink"/> property or
        /// a link to a general troubleshooting page if the property
        /// is not assigned.
        /// </summary>
        /// <returns>The assigned help link value or the default.</returns>
        internal string GetHelplinkOrDefault() {
            // Come up with some good general troubleshooting links.
            // We should decide which to use based on the source of
            // this task, if a URL is not already set. The general
            // help page for deployment can discuss different issues
            // than the one for debugging, etc.
            // TODO:

            return string.IsNullOrEmpty(this.Helplink)
                ? "http://www.starcounter.com/wiki"
                : this.Helplink;
        }

        private void BindToErrorMessage(ErrorMessage message) {
            this.Text = string.Format("{0} ({1})", message.Body, message.Header);
            this.Helplink = message.Helplink;
            this.ErrorMessage = message;
        }
    }
}
