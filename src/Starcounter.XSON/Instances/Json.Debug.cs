// ***********************************************************************
// <copyright file="App.Json.cs" company="Starcounter AB">
//     Copyright (c) Starcounter AB.  All rights reserved.
// </copyright>
// ***********************************************************************


using Starcounter.Templates;
using System;
using System.Text;
namespace Starcounter {

    public partial class Json {

        internal string DebugString {
            get {
                var sb = new StringBuilder();
                WriteToDebugString(sb, 0);
                return sb.ToString();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="i"></param>
        internal override void WriteToDebugString(StringBuilder sb, int i) {
            if (this.IsArray) {
                throw new NotImplementedException();
//                WriteToDebugString(sb, i, (ArrSchema<Json>)Template);
            }
            else {
                WriteToDebugString(sb, i, (TObject)Template);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="i"></param>
        /// <param name="template"></param>
        internal void WriteToDebugString(StringBuilder sb, int i, TObject template ) {

            _WriteDebugProperty(sb);

            if (Template == null) {
                sb.Append("{}");
                return;
            }
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
                    var prop = template.Properties[t];
                    sb.Append('"');
                    sb.Append(prop.PropertyName);
                    sb.Append("\":");
                    if (prop is TValue && ((TValue)prop).Bind != null) {
                        var tv = (TValue)prop;
                        if (this.Get(tv) != _BoundDirtyCheck[t]) {
                            var dbgVal = _BoundDirtyCheck[t];
                            if (dbgVal == null)
                                dbgVal = "notsent";
                            sb.Append("(db d=" + dbgVal + ")");
                        }
                        else {
                            sb.Append("(db)");
                        }
                    }
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