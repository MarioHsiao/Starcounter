using System.Collections.Generic;
using Starcounter.Templates.Interfaces;
#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif

//    public class SetProperty<AppType, SchemaType> : AppListTemplate<AppType> where AppType : App, new() where SchemaType : AppTemplate {
//    }

    public class ListingProperty<AppType,AppTemplateType> : ListingProperty
        where AppType : App, new()
        where AppTemplateType : AppTemplate
    {
        public override object CreateInstance(AppNode parent) {
            return new Listing<AppType>((App)parent, this);
        }

        public override System.Type InstanceType {
            get { return typeof(Listing<AppType>); }
        }

        public new AppTemplateType App {
            get {
                return (AppTemplateType)(base.App);
            }
            set {
                base.App = value;
            }
        }

    }

    public class ListingProperty : ListTemplate
#if IAPP
        , IAppListTemplate
#endif
    {
        internal AppTemplate[] _Single = new AppTemplate[0]; 

        IAppTemplate IAppListTemplate.Type {
            get {
                return App;
            }
            set {
                App = (AppTemplate)value;
            }
        }

        public AppTemplate App {
            get {
                if (_Single.Length == 0)
                    return null;
                return (AppTemplate)_Single[0];
            }
            set {
                _Single = new AppTemplate[1];
                 _Single[0] = (AppTemplate)value;
            }
        }

        public override object DefaultValueAsObject {
            get {
                throw new System.NotImplementedException();
            }
            set {
                throw new System.NotImplementedException();
            }
        }

        public override object CreateInstance( AppNode parent ) {
            return new Listing( (App)parent, this );
        }

        public override System.Type InstanceType {
            get { return typeof(Listing); }
        }

        public override IEnumerable<Template> Children {
            get {
                return (IEnumerable<Template>)_Single;
            }
        }
    }

}
