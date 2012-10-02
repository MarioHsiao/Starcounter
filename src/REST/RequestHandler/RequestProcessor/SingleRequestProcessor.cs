
using System;
namespace Starcounter.Internal.Uri {

    public abstract class SingleRequestProcessorBase : RequestProcessor {
        //        public abstract override bool Process(byte[] fragment, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out App resource );
        public abstract object CodeAsObj { get; set; }
    }

    public abstract class SingleRequestProcessor : SingleRequestProcessorBase {
        public Func<object> Code { get; set; }
        public override object CodeAsObj { get { return Code; } set { Code = (Func<object>)value; } }

        //public override App Invoke(byte[] uri, HttpRequest request) {
        //    return Code.Invoke();
        //}
    }

    public abstract class SingleRequestProcessor<T> : SingleRequestProcessorBase {
        public Func<T, object> Code { get; set; }
        public override object CodeAsObj { get { return Code; } set { Code = (Func<T, object>)value; } }
    }

    public abstract class SingleRequestProcessor<T1, T2> : SingleRequestProcessorBase {
        public Func<T1, T2, object> Code { get; set; }
        public override object CodeAsObj { get { return Code; } set { Code = (Func<T1, T2, object>)value; } }
    }

    public abstract class SingleRequestProcessor<T1, T2, T3> : SingleRequestProcessorBase {
        public Func<T1, T2, T3, object> Code { get; set; }
        public override object CodeAsObj { get { return Code; } set { Code = (Func<T1, T2, T3, object>)value; } }
    }
}