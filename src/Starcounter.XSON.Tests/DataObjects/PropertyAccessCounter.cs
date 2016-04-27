using System;

namespace Starcounter.Internal.XSON.Tests {
    public class PropertyAccessCounter {
        private string name;
        
        public string Name {
            get {
                GetNameCount++;
                return name;
            }
            set {
                SetNameCount++;
                name = value;
            }
        }

        public string NameSkipCounter { get { return name; } }
        public int GetNameCount { get; private set; }
        public int SetNameCount { get; private set; }

        public void ResetCount() {
            GetNameCount = 0;
            SetNameCount = 0;
        }
    }
}
