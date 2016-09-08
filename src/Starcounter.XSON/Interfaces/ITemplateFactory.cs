using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Starcounter.Templates;
using Starcounter.XSON.JSONByExample;

namespace Starcounter.XSON.Interfaces {
    /// <summary>
    /// 
    /// </summary>
    interface ITemplateFactory {

        // object AddAppProperty(object parent, string name, string dotNetName, ISourceInfo debugInfo);
        TObject AddObject(Template parent, string name, string dotNetName, ISourceInfo sourceInfo);

        //object AddTString(object parent, string name, string dotNetName, string value, ISourceInfo debugInfo);
        TString AddString(Template parent, string name, string dotNetName, string value, ISourceInfo sourceInfo);

        //object AddIntegerProperty(object parent, string name, string dotNetName, int value, ISourceInfo debugInfo);
        TLong AddInteger(Template parent, string name, string dotNetName, int value, ISourceInfo sourceInfo);

        //object AddTDecimal(object parent, string name, string dotNetName, decimal value, ISourceInfo debugInfo);
        TDecimal AddDecimal(Template parent, string name, string dotNetName, decimal value, ISourceInfo sourceInfo);

        //object AddTDouble(object parent, string name, string dotNetName, double value, ISourceInfo debugInfo);
        TDouble AddDouble(Template parent, string name, string dotNetName, double value, ISourceInfo debugInfo);

        //object AddBooleanProperty(object parent, string name, string dotNetName, int value, ISourceInfo debugInfo);
        TBool AddBoolean(Template parent, string name, string dotNetName, bool value, ISourceInfo sourceInfo);

        //object AddArrayProperty(object parent, string name, string dotNetName, int value, ISourceInfo debugInfo);
        TObjArr AddArrayProperty(Template parent, string name, string dotNetName, ISourceInfo sourceInfo);

        // REMOVE
        //object AddEventProperty(object parent, string name, string dotNetName, int value, ISourceInfo debugInfo);

        // REMOVE
        //object AddCargoProperty(object parent, string name, string dotNetName, int value, ISourceInfo debugInfo);

        // REMOVE?
        //object AddMetaProperty(object template, ISourceInfo debugInfo);

        //object GetMetaTemplate(object template, ISourceInfo debugInfo);
        MetaTemplate GetMetaTemplate(Template template, ISourceInfo sourceInfo);

        //object GetMetaTemplateForProperty(object template, string name, ISourceInfo debugInfo);
        MetaTemplate GetMetaTemplate(Template parent, string propertyName, ISourceInfo debugInfo);

        // REMOVE
        //void SetEditableProperty(object template, bool b, ISourceInfo debugInfo);
        //void SetClassProperty(object template, string className, ISourceInfo debugInfo);
        //void SetIncludeProperty(object template, string className, ISourceInfo debugInfo);
        //void SetNamespaceProperty(object template, string namespaceName, ISourceInfo debugInfo);
        //void SetOnUpdateProperty(object template, string functionName, ISourceInfo debugInfo);
        //void SetBindProperty(object template, string path, ISourceInfo debugInfo);
    }
}
