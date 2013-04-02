using System;
using System.Text;
using System.Threading;
using Starcounter;
using System.Diagnostics;

namespace SqlCacheTrasher
{
    // Simple class that is used for testing SQL cache functionality.
    [Database]
    public class SimpleObject
    {
        public Int64 IntegerProperty;

        // Simple constructor.
        public SimpleObject(Int64 integerProperty)
        {
            this.IntegerProperty = integerProperty;
        }
    }
}