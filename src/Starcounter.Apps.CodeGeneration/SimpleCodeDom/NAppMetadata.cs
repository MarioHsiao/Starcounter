﻿

using Starcounter.Templates;
using System.Collections.Generic;
namespace Starcounter.Internal.Application.CodeGeneration {

    /// <summary>
    /// Each App can have a metadata class. See AppMetadata.
    /// </summary>
    public class NAppMetadata : NClass {
        public NApp AppNode;
        public AppTemplate Template;

        public static Dictionary<AppTemplate, NClass> Instances = new Dictionary<AppTemplate, NClass>();

        public override string ClassName {
            get {
                return AppNode.ClassName + "Metadata";
            }
        }

        public string _Inherits;

        public override string Inherits {
            get { return _Inherits; }
        }

    }
}
