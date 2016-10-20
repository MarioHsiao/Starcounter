using System;
using Starcounter.XSON.Interfaces;

namespace Starcounter.XSON.PartialClassGenerator {
    public class GeneratorException : Exception {
        private GeneratorException() : base() {
        }

        public GeneratorException(string message, ISourceInfo sourceInfo) : base(message) {
            SourceInfo = sourceInfo;
        }

        public GeneratorException(string message, ISourceInfo sourceInfo, Exception innerException)
            : base(message, innerException) {
            SourceInfo = sourceInfo;
        }

        public ISourceInfo SourceInfo {
            get; private set;
        }
    }

    public class GeneratorWarning : ITemplateCodeGeneratorWarning {
        private GeneratorWarning() {
        }

        public GeneratorWarning(string warning, ISourceInfo sourceInfo) {
            Warning = warning;
            SourceInfo = sourceInfo;
        }
        
        public ISourceInfo SourceInfo { get; private set; }

        public string Warning { get; private set; }
    }
}
