using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Starcounter.TransactionLog
{
    class MetadataCache
    {
        public void notify_on_new_generation(ulong gen)
        {
            if (generation < gen)
            {
                names.Clear();
                generation = gen;
            }
        }

        public string this[IntPtr ptr]
        {
            get
            {
                string name;

                if (!names.TryGetValue(ptr.ToInt64(), out name))
                {
                    name = Marshal.PtrToStringUni(ptr);
                    names[ptr.ToInt64()] = name;
                }

                return name;
            }
        }

        private Dictionary<long, string> names = new Dictionary<long, string>();
        private ulong generation;
    }
}
