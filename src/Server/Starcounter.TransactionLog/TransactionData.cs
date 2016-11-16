using System;
using System.Collections.Generic;

namespace Starcounter.TransactionLog
{
    public struct Reference
    {
        public ulong ObjectID;
    }

    public struct ColumnUpdate
    {
        public string Name;
        private object value_;
        public object Value //reference, sring, long, ulong, decimal, float, double, byte[]
        {
            get { return (value_ as Lazy<string>)?.Value ?? value_; }
            set { value_ = value; }
        }
    };

    public struct CreateRecordEntry
    {
        public string Table;
        public Reference Key;
        public ColumnUpdate[] Columns;
    };

    public struct UpdateRecordEntry
    {
        public string Table;
        public Reference Key;
        public ColumnUpdate[] Columns;
    };

    public struct DeleteRecordEntry
    {
        public string Table;
        public Reference Key;
    };

    public class TransactionData
    {
        public List<CreateRecordEntry> Creates;
        public List<UpdateRecordEntry> Updates;
        public List<DeleteRecordEntry> Deletes;
    }
}
