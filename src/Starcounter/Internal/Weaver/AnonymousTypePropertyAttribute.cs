using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Starcounter;

namespace Starcounter.Internal.Weaver {
    
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    internal sealed class AnonymousTypePropertyAttribute : Attribute {
        private int index;

        public AnonymousTypePropertyAttribute(int index) {
            this.index = index;
        }

        public int Index {
            get {
                return this.index;
            }
        }
    }
}