using System;

namespace Starcounter.Internal.XSON.Tests {
    public class PropertyAccessCounter {
        private string name;
        private Agent agent;
        
        public string Name {
            get {
                GetNameCount++;
                return name;
            }
            set {
                SetNameCount++;
                name = value;
            }
        }

        public Agent Agent {
            get {
                GetAgentCount++;
                return agent;
            }
            set {
                SetAgentCount++;
                agent = value;
            }
        }
        
        public string NameSkipCounter { get { return name; } }
        public int GetNameCount { get; private set; }
        public int SetNameCount { get; private set; }

        public Agent AgentSkipCounter { get { return agent; } }
        public int GetAgentCount { get; private set; }
        public int SetAgentCount { get; private set; }

        public void ResetCount() {
            GetNameCount = 0;
            SetNameCount = 0;
            GetAgentCount = 0;
            SetAgentCount = 0;
        }
    }
}
