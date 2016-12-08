using System;
using System.Text;

namespace Starcounter {

    public class Partial : Page {
        public string ImplicitStandaloneTitle = Application.Current.DisplayName;
        private byte[] ImplicitStandalonePageBytes;
        
        public Partial() {
            if (Session.Current == null) {
                this.Session = new Session(SessionOptions.PatchVersioning);
            }
        }
        
        public override byte[] AsMimeType(MimeType mimeType, out MimeType resultingMimeType, Request request = null) {

            resultingMimeType = mimeType;

            if (mimeType == MimeType.Text_Html) {
                var bytes = HtmlFromJsonProvider.ProvideFromFilePath<byte[]>(Html);
                if (PartialToStandaloneHtmlProvider.IsFullPageHtml(bytes)) {
                    return bytes;
                }

                if (ImplicitStandalonePageBytes == null) {
                    ImplicitStandalonePageBytes = PartialToStandaloneHtmlProvider.ProvideImplicitStandalonePage(bytes, ImplicitStandaloneTitle, Session.Current?.SessionUri);
                }

                return ImplicitStandalonePageBytes;
            }

            return base.AsMimeType(mimeType, out resultingMimeType, request);
        }
    }
}