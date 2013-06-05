using System;
using Starcounter.Advanced;

namespace Starcounter.XSON.Tests {
    /// <summary>
    /// 
    /// </summary>
    public abstract class SimpleBase : IBindable {
        /// <summary>
        /// 
        /// </summary>
        public string BaseValue { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public virtual string VirtualValue { get { return "Base"; } }
        /// <summary>
        /// 
        /// </summary>
        public abstract string AbstractValue { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public ulong Identity { get { return 0; } }
    }

    /// <summary>
    /// 
    /// </summary>
    public class SubClass1 : SimpleBase {
        private string abstractValue;
        /// <summary>
        /// 
        /// </summary>
        public override string AbstractValue {
            get { return abstractValue; }
            set { abstractValue = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        public override string VirtualValue {
            get { return "SubClass1"; }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class SubClass2 : SimpleBase {
        private string abstractValue;
        /// <summary>
        /// 
        /// </summary>
        public override string AbstractValue {
            get { return abstractValue; }
            set { abstractValue = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        public override string VirtualValue {
            get { return "SubClass2"; }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class SubClass3 : SubClass2 {
        private string abstractValue;
        /// <summary>
        /// 
        /// </summary>
        public override string AbstractValue {
            get { return abstractValue; }
            set { abstractValue = value; }
        }
        /// <summary>
        /// 
        /// </summary>
        public override string VirtualValue {
            get { return "SubClass3"; }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    public class PersonObject : BasePerson {
        /// <summary>
        /// 
        /// </summary>
        public int Age { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public PhoneNumberObject Number { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Misc { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public class PhoneNumberObject : IBindable {
        /// <summary>
        /// 
        /// </summary>
        public ulong Identity {
            get { return 0; }
        }
        /// <summary>
        /// 
        /// </summary>
        public string Number { get; set; }
    }

    /// <summary>
    /// 
    /// </summary>
    public abstract class BasePerson : IBindable {
        /// <summary>
        /// 
        /// </summary>
        public BasePerson() {
            Created = DateTime.Now;
        }
        /// <summary>
        /// 
        /// </summary>
        public ulong Identity {
            get { return 0; }
        }
        /// <summary>
        /// 
        /// </summary>
        public string FirstName { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string Surname { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime Created { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public DateTime Updated { get; set; }
    }
}
