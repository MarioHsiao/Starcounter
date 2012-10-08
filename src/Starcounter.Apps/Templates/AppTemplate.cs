using System;
using System.Collections.Generic;
using Starcounter.Templates.Interfaces;

#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif

    /// <summary>
    /// Defines the properties of an App instance.
    /// </summary>
    public class AppTemplate : ParentTemplate
#if IAPP
        , IAppTemplate
#endif
    {

        public TTemplate Register<TApp, TTemplate>(string name, bool editable = false, Func<TApp, TTemplate, Input<TApp, TTemplate>> input = null)
            where TTemplate : Template, new()
            where TApp : App {
            return new TTemplate() {
                Parent = this,
                Name = name,
                Editable = editable,
                InputHandler = input
            };
        }

        public TTemplate Register<TApp, TTemplate, TValue>(string name, bool editable = false, Func<TApp, TTemplate, Input<TApp, TTemplate, TValue>> input = null)
            where TTemplate : Template, new()
            where TApp : App {
            return new TTemplate() {
                Parent = this,
                Name = name,
                Editable = editable,
                InputHandler = input
            };
        }

        internal string _ClassName;

        public string ClassName {
            get {
                return _ClassName;
            }
            set {
                _ClassName = value;
            }
        }

        public string Namespace { get; set; }
        public string Include { get; set; }


        private PropertyList _PropertyTemplates;

        public AppTemplate() {
            _PropertyTemplates = new PropertyList(this);
        }

        private Type _AppType;

        public override Type InstanceType {
            get {
                if (_AppType == null) {
                    return typeof(App);
                }
                return _AppType;
            }
            set { _AppType = value; }
        }


        public T Add<T>(string name) where T : ITemplate, new() {
            T t = new T() { Name = name };
            Properties.Add(t);
            return t;
        }

        public T Add<T>(string name, IAppTemplate type ) where T : IAppListTemplate, new() {
            T t = new T() { Name = name, Type = type };
            Properties.Add(t);
            return t;
        }

        public PropertyList Properties { get { return _PropertyTemplates; } }

        public override IEnumerable<Template> Children {
            get { return (IEnumerable<Template>)Properties; }
        }

        public override object CreateInstance( AppNode parent ) {
            return new App() { Template = this, Parent = parent };
        }

        T IAppTemplate.Add<T>(string name) {
            throw new NotImplementedException();
        }

        T IAppTemplate.Add<T>(string name, IAppTemplate type) {
            throw new NotImplementedException();
        }

        IPropertyTemplates IAppTemplate.Properties {
            get { return Properties; }
        }


    }

}
