

using Starcounter.Templates;
using System;
using System.Collections.Generic;
using System.Text;
namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// The source code representation of each Listing<T1>, ListingProperty<T1,T2> 
    /// or ListingMetadata<T1,T2> class where 
    /// T1 is the link to the App class and T2 is the link to the AppTemplate class being used in the list.
    /// This means that there is one instance of this class for each T1,T2 combination used.
    /// </summary>
    public class NListingXXXClass : NClass {

        public NListingXXXClass(string typename, NClass appType, NClass templateType) {
            TypeName = typename;
            NApp = appType;
            NAppTemplate = templateType;
        }

        /// <summary>
        /// The type of the App
        /// </summary>
        public NClass NApp;

        /// <summary>
        /// The typeof the AppTemplate
        /// </summary>
        public NClass NAppTemplate;

     //   public NPredefinedClass NFixedSet;

        public string TypeName;

        public override string Inherits {
            get { throw new NotImplementedException(); }
        }

        /// <summary>
        /// The class name is "ListingXXX<AppClass,AppTemplateClass>"
        /// </summary>
        public override string ClassName {
            get {
                var sb = new StringBuilder();
                sb.Append(TypeName);
                sb.Append('<');
                sb.Append( NApp.FullClassName );
                if (NAppTemplate != null) {
                    sb.Append(", ");
                    sb.Append(NAppTemplate.FullClassName);
                }
                sb.Append('>');
                return sb.ToString();
            }
        }

    }
}
