
using System;
using Starcounter.XSON.Interfaces;

namespace Starcounter.XSON.Templates.Factory {
    public class SourceInfo : ISourceInfo {
        public string Filename {
            get; internal set;
        }

        public int Line {
            get; internal set;
        }

        public int Column {
            get; internal set;
        }
    }
}
