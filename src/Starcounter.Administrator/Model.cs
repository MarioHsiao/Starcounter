using System;
using Sc.Tools.Logging;
using Starcounter;

namespace StarcounterApps3 {
    public class Class1 : Entity {
        public string Field1;
    }

    public class LogItem : Entity {
        public long SeqNumber; // TODO: Should be ulong
        public long ActivityID;
        public string Category;
        public DateTime DateTime;

        public string DateTimeStr {
            get { return DateTime.ToString(); }
        }

        public string MachineName;
        public string Message;
        public string ServerName;
        public string Source;
        public EntryType Type;
        public string TypeStr {
            get { return Type.ToString(); }
        }
        public string UserName;
    }
}