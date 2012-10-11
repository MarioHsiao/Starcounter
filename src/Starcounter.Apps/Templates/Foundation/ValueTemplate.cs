
using System;
using Starcounter.Templates.Interfaces;
#if CLIENT
using Starcounter.Client;
namespace Starcounter.Client.Template {
#else
using Starcounter.Templates;
using System.Collections.Generic;
namespace Starcounter {
#endif

    public abstract class Property<TValue> : Property {
        public Func<App, Property<TValue>, TValue, Input<TValue>> CustomInputEventCreator = null;
        public List<Action<App,Input<TValue>>> CustomInputHandlers = new List<Action<App,Input<TValue>>>();


        public void AddHandler(
            Func<App, Property<TValue>, TValue, Input<TValue>> createInputEvent = null,
            Action<App, Input<TValue>> handler = null) {
            this.CustomInputEventCreator = createInputEvent;
            this.CustomInputHandlers.Add(handler);
        }
    }   
 
 
    public abstract class Property : Template
#if IAPP
        , IValueTemplate
#endif
    {

        public override bool HasInstanceValueOnClient {
            get { return true; }
        }
    }
}
