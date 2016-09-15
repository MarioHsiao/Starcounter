
using System;

namespace Starcounter.UnitTesting.Runtime
{
    public class ResultHeader
    {
        public string UniqueId { get; private set; }
        public string Database { get; private set; }
        public DateTime Start { get; private set; }
        
        public static ResultHeader NewStartingNow(string database)
        {
            return new ResultHeader()
            {
                UniqueId = Guid.NewGuid().ToString(),
                Database = database,
                Start = DateTime.Now
            };
        }
    }
}
