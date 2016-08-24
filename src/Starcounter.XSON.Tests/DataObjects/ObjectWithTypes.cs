using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace Starcounter.Internal.XSON.Tests {
    public enum ShortEnum : short {
        Value1,
        Value2
    }

    public enum IntEnum : int {
        Value1,
        Value2
    }

    public class ObjectWithTypes {
        public bool Value1;
        public int Value2;
        public long Value3;
        public double Value4;
        public decimal Value5;
        public string Value6;

        public ShortEnum Value7;
        public IntEnum Value8;
        
        public Entity Value9;
        public IList Value10;
        public List<Entity> Value11;
    }
}
