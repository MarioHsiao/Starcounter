
using System;
using System.Runtime.InteropServices;
using System.Text;
using Starcounter.Internal;
using Starcounter.Templates;

namespace Starcounter.XSON.Serializers {
    public class DefaultSerializer : TypedJsonSerializerBase {
        public static readonly TypedJsonSerializer Instance = new DefaultSerializer();

        public override int PopulateFromJson(Obj obj, IntPtr buffer, int jsonSize) {
            string propertyName;
            var reader = new JsonReader(buffer, jsonSize);
            Arr arr;
            Obj childObj;
            TObj tObj = obj.Template;
            Template tProperty;

            while (reader.GotoProperty()) {
                propertyName = reader.ReadString();
                tProperty = tObj.Properties.GetTemplateByName(propertyName);
                if (tProperty == null) {
                    throw ErrorCode.ToException(Error.SCERRJSONPROPERTYNOTFOUND, string.Format("Property=\"{0}\""));
                }

                reader.GotoValue();
                try {
                    if (tProperty is TBool) {
                        obj.Set((TBool)tProperty, reader.ReadBool());
                    } else if (tProperty is TDecimal) {
                        obj.Set((TDecimal)tProperty, reader.ReadDecimal());
                    } else if (tProperty is TDouble) {
                        obj.Set((TDouble)tProperty, reader.ReadDouble());
                    } else if (tProperty is TLong) {
                        obj.Set((TLong)tProperty, reader.ReadLong());
                    } else if (tProperty is TString) {
                        obj.Set((TString)tProperty, reader.ReadString());
                    } else if (tProperty is TObj) {
                        childObj = obj.Get((TObj)tProperty);
                        reader.PopulateObject(childObj);
                    } else if (tProperty is TObjArr) {
                        arr = obj.Get((TObjArr)tProperty);
                        while (reader.GotoNextObjectInArray()) {
                            childObj = arr.Add();
                            reader.PopulateObject(childObj);
                        }
                    }
                } catch (Exception ex) {
                    throw ErrorCode.ToException(
                            Error.SCERRJSONVALUEWRONGTYPE,
                            ex,
                            string.Format("Property=\"{0} ({1})\", Value=\"{2}\"", tProperty.PropertyName, tProperty.JsonType));
                }
            }
            return reader.Offset;
        }
    }
}