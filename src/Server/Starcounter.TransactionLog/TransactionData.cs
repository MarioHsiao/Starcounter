using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.TransactionLog
{
    public struct reference
    {
        public ulong object_id;
    }

    public struct column_update
    {
        public string name;
        private object value_;
        public object value //reference, sring, long, ulong, decimal, float, double, byte[]
        {
            get { return (value_ as Lazy<string>)?.Value ?? value_; }
            set { value_ = value; }
        }
    };

    public struct create_record_entry
    {
        public string table;
        public reference key;
        public column_update[] columns;
    };

    public struct update_record_entry
    {
        public string table;
        public reference key;
        public column_update[] columns;
    };

    public struct delete_record_entry
    {
        public string table;
        public reference key;
    };

    public class TransactionData
    {
        public List<create_record_entry> creates;
        public List<update_record_entry> updates;
        public List<delete_record_entry> deletes;
    }

    class MetadataCache
    {
        public void notify_on_new_generation(ulong gen)
        {
            if ( generation < gen )
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

                    if ( name.Where(c=>c>255).Any() )
                    {
                        System.Diagnostics.Debugger.Launch();
                    }
                }

                return name;
            }
        }

        private Dictionary<long, string> names = new Dictionary<long, string>();
        private ulong generation;
    }

}
