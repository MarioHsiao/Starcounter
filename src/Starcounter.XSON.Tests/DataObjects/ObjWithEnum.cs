using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Internal.XSON.Tests {
    public class ObjWithEnum : Entity {
        public TestEnum TestEnum { get; set; }
    }

    public enum TestEnum {
        First,
        Second,
        Third
    }
}
