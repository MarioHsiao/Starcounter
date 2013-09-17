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

        internal void WriteToDebugString(StringBuilder sb, int indentation) {
            if (IsArray) {
                WriteArrayToDebugString(sb, indentation);
            }
            else {
                WriteObjectToDebugString(sb, indentation);
            }
        }

        private void WriteArrayToDebugString(StringBuilder sb, int indentation) {
            _WriteDebugProperty(sb);

            sb.Append("[");
            indentation += 3;
            int t = 0;
            var values = list;
            foreach (var e in values) {
                if (t > 0) {
                    sb.AppendLine(",");
                    sb.Append(' ', indentation);
                }
                (e as Json).WriteToDebugString(sb, indentation);
                t++;
            }
            indentation -= 3;
            sb.AppendLine();
            sb.Append(' ', indentation);
            sb.Append("]");
        }   
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
        private void WriteObjectToDebugString(StringBuilder sb, int i) {
            if (this.IsArray) {
                throw new NotImplementedException();
//                WriteToDebugString(sb, i, (ArrSchema<Json>)Template);
            }
            else {
                WriteObjectToDebugString(sb, i, (TObject)Template);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sb"></param>
        /// <param name="i"></param>
        /// <param name="template"></param>
        private void WriteObjectToDebugString(StringBuilder sb, int i, TObject template ) {

            _WriteDebugProperty(sb);

            if (Template == null) {
                sb.Append("{}");
                return;
            }

            if (Session != null && Parent != null) {
                var index = Parent.IndexOf(this);
                if (Parent._ReplacedFlag[index]) {
                    sb.Append("(set)");
                }
            }


            sb.AppendLine("{");


            i += 3;
            int t = 0;
            var vals = list;
            foreach (var v in vals) {
                if (t > 0) {
                    sb.AppendLine(",");
                }
                sb.Append(' ', i);
                if (v is Json) {
                    (v as Json).WriteToDebugString(sb, i);
                }
                else {
                    var prop = template.Properties[t];
                    sb.Append('"');
                    sb.Append(prop.PropertyName);
                    sb.Append("\":");
                    if (prop is TValue && ((TValue)prop).Bind != null) {
                        var tv = (TValue)prop;
                        if ((this as Json).Get(tv) != _list[t]) {
                            var dbgVal = _list[t];
                            if (dbgVal == null)
                                dbgVal = "notsent";
                            sb.Append("(db old=" + dbgVal + ")");
                        }
                        else {
                            sb.Append("(db)");
                        }
                    }
                    if (WasReplacedAt(t)) {
                        if (prop is TContainer) {
                            // The "d" is already anotated for Containers.
                            // Let's just make sure that the dirty flag was the same
                            var obj = (Json)(this as Json).Get((TValue)prop);
                            if (!obj._Dirty) {
                                throw new Exception("Missmach in dirty flags");
                            }
                        }
                        else {
                            sb.Append("(dirty=\"" + v + "\")");
                        }
                    }
                    sb.Append(Newtonsoft.Json.JsonConvert.SerializeObject((this as Json).Get((TValue)prop)));
                }
                t++;
            }
            i -= 3;
            sb.AppendLine();
            sb.Append(' ', i);
           sb.Append("}");
        }


        /// <summary>
        /// Called by WriteDebugToString implementations
        /// </summary>
        /// <param name="sb">The string used to write text to</param>
        internal void _WriteDebugProperty(StringBuilder sb) {
            var t = this.Template;
            if (t != null) {
                var name = this.Template.PropertyName;
                if (name != null) {
                    sb.Append('"');
                    sb.Append(name);
                    sb.Append("\":");
                }
            }
            if (this is Json && ((Json)this).Data != null) {
                sb.Append("(db)");
            }
            if (list == null || _BrandNew) {
                sb.Append("(new)");
            }
            if (_Dirty) {
                sb.Append("(dirty)");
            }
        }
    }
}