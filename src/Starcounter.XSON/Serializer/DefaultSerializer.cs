
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
                propertyName = reader.CurrentPropertyName;
                tProperty = tObj.Properties.GetTemplateByName(propertyName);
                if (tProperty == null || !tObj.Properties.IsExposed(tProperty)) {
                    JsonHelper.ThrowPropertyNotFoundException(propertyName);
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
                } catch (InvalidCastException ex) {
                    JsonHelper.ThrowWrongValueTypeException(ex, tProperty.TemplateName, tProperty.JsonType, reader.ReadString());
                }
            }
            return reader.Used;
        }
    }
}