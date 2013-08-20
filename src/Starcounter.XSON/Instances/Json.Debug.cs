// ***********************************************************************
// <copyright file="App.Json.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************


using Starcounter.Templates;
using System;
using System.Text;
namespace Starcounter {

    public partial class Obj {

        internal string DebugString {
            get {
                var sb = new StringBuilder();
                WriteToDebugString(sb, 0);
                return sb.ToString();
            }
        }

        internal override void WriteToDebugString(StringBuilder sb, int i) {
            if (Template == null) {
                sb.Append("{}");
                return;
            }

            _WriteDebugProperty(sb);


            sb.AppendLine("{");


            i += 3;
            int t = 0;
            var vals = Values;
            foreach (var v in vals) {
                if (t > 0) {
                    sb.AppendLine(",");
                }
                sb.Append(' ', i);
                if (v is Container) {
                    v.WriteToDebugString(sb, i);
                }
                else {
                    var prop = this.Template.Properties[t];
                    sb.Append('"');
                    sb.Append(prop.PropertyName);
                    sb.Append("\":");
                    if (_DirtyProperties[t]) {
                        if (prop is TContainer) {
                            // The "d" is already anotated for Containers.
                            // Let's just make sure that the dirty flag was the same
                            var obj = (Container)this.Get((TValue)prop);
                            if (!obj._Dirty) {
                                throw new Exception("Missmach in dirty flags");
                            }
                        }
                        else {
                            sb.Append("(d\"" + v + "\")");
                        }
                    }
                    sb.Append(Newtonsoft.Json.JsonConvert.SerializeObject(this.Get((TValue)prop)));
                }
                t++;
            }
            i -= 3;
            sb.AppendLine();
            sb.Append(' ', i);
           sb.Append("}");
        }

    }
}