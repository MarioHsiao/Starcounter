using System;
using Starcounter.XSON.Interfaces;

namespace Starcounter.XSON.Templates.Factory {
    public class TemplateFactoryException : Exception {
        private TemplateFactoryException() : base() {
        }

        public TemplateFactoryException(string message, ISourceInfo sourceInfo) : base(message) {
            SourceInfo = sourceInfo;
        }

        public TemplateFactoryException(string message, ISourceInfo sourceInfo, Exception innerException)
            : base(message, innerException) {
            SourceInfo = sourceInfo;
        }

        public ISourceInfo SourceInfo {
            get; private set;
        }
    }
}
