
using System;

namespace Starcounter.Errors
{
    public sealed class Facility
    {
        internal Facility(string name, uint code)
        {
            if (code >> 12 != 0)
                throw new ArgumentOutOfRangeException("code", code, "Not a valid 12-bit value: 0x" + code.ToString("X"));

            this.Name = name;
            this.Code = code;
        }

        public string Name
        {
            get;
            set;
        }
        
        public uint Code
        {
            get;
            set;
        }
    }
}