using System;
using System.Collections.Generic;
using Starcounter.Templates.Interfaces;
#if CLIENT
namespace Starcounter.Client.Template {
#else
namespace Starcounter.Templates {
#endif


    public abstract class ParentTemplate : Property
#if IAPP
        , IParentTemplate 
#endif
    {
        private bool _Sealed;

        public override bool Sealed {
            get {
                return _Sealed;
            }
            internal set {
                if (!value && _Sealed) {
                    throw new Exception("Once a AppTemplate is sealed, you cannot unseal it");
                }
                _Sealed = value;
            }
        }

        public abstract IEnumerable<Template> Children { get; }

     }

}
