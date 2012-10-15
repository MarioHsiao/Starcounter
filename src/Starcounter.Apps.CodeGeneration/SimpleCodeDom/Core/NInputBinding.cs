using System;
using System.Text;

namespace Starcounter.Internal.Application.CodeGeneration
{
    /// <summary>
    /// Defines an input handler in one app-class for a specified property.
    /// The handler can be declared either in the same class as the property
    /// or a parent app or in several places where each handler is called 
    /// if the first one didn't handle it.
    /// </summary>
    public class NInputBinding : NBase
    {
        /// <summary>
        /// The property this binding binds to
        /// </summary>
        public NProperty BindsToProperty { get; set; }

        /// <summary>
        /// The app where the property is declared.
        /// </summary>
        public NAppClass PropertyAppClass { get; set; }

        /// <summary>
        /// The App that declares the Handle-method. Might not be the same
        /// app as the property is declared in.
        /// </summary>
        public NAppClass DeclaringAppClass { get; set; }

        /// <summary>
        /// Count on how many parent calls are needed to go from the property
        /// appclass to the class where the handle method is declared.
        /// </summary>
        public Int32 AppParentCount { get; set; }

        /// <summary>
        /// The full name of the Input-type for this binding.
        /// </summary>
        public String InputTypeName { get; set; }

        public NInputBinding()
        {

        }

        //public String GetBindingCode()
        //{
        //    StringBuilder sb = new StringBuilder();
        //    sb.Append("        ");
        //    sb.Append(BindsToProperty.Template.Name);       // {0}
        //    sb.Append(".AddHandler((App app, Property<");
        //    sb.Append(BindsToProperty.Template.JsonType);   // {1}
        //    sb.Append("> prop, ");
        //    sb.Append(BindsToProperty.Template.JsonType);   // {1}
        //    sb.Append(" value) => { return (new ");
        //    sb.Append(InputTypeName);                       // {2}
        //    sb.Append("() { App = (");
        //    sb.Append(PropertyAppClass.ClassName);          // {3}
        //    sb.Append(")app, Template = (");
        //    sb.Append(BindsToProperty.Type.ClassName);      // {4}
        //    sb.Append(")prop, Value = value }); }, (App app, Input<");
        //    sb.Append(BindsToProperty.Template.JsonType);   // {1}
        //    sb.Append("> Input) => { ((");
        //    sb.Append(DeclaringAppClass.ClassName);         // {5}
        //    sb.Append(")app");

        //    for (Int32 i = 0; i < AppParentCount; i++)
        //    {
        //        sb.Append(".Parent");
        //    }

        //    sb.Append(").Handle((");
        //    sb.Append(InputTypeName);                       // {2}
        //    sb.Append(")Input); });");

        //    return sb.ToString();
        //}
    }
}

//Input handler registration code:
//{0} : PropertyName.
//{1} : Datatype.
//{2} : Inputname (from metadata).
//{3} : Classname of App where the property is declared.
//{4} : TemplateTypeName.
//{5} : Classname of the class where Handle is declared.
//{6} : Parent calls to go from the class where the property is 
//      declared to the class where the Handle method is declared.


//{0}.AddHandler
//(
//    (App app, Property<{1}> prop, {1} value) =>
//    {
//        return
//        (
//            new {2}()
//            {
//                App = ({3})app,
//                Template = ({4})prop,
//                Value = value
//            }
//        );
//    },
//    (App app, Input<{1}> Input) =>
//    {
//        (({5})app.{6}).Handle(({2})Input);
//    }
//);


