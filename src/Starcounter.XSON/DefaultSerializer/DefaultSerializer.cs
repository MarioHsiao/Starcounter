
using System;
using System.Runtime.InteropServices;
using System.Text;
using Starcounter.Internal;
using Starcounter.Templates;

namespace Starcounter.Advanced.XSON {

    public class DefaultSerializer : TypedJsonSerializerBase {
        public static readonly TypedJsonSerializer Instance = new DefaultSerializer();

        public override int PopulateFromJson(Json<object> obj, IntPtr buffer, int jsonSize) {
            string propertyName;

            if (obj.IsArray) {
                throw new NotImplementedException("Cannot serialize JSON where the root object is an array");
            }

            var reader = new JsonReader(buffer, jsonSize);
            Arr<Json<object>> arr;
            Json<object> childObj;
            Schema<Json<object>> tObj = (Schema<Json<object>>)obj.Template;
            Template tProperty;

            while (reader.GotoProperty()) {
                propertyName = reader.CurrentPropertyName;
                tProperty = tObj.Properties.GetExposedTemplateByName(propertyName);
                if (tProperty == null) {
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
                    }
                    else if (tProperty is Schema<Json<object>>) {
                        childObj = obj.Get((Schema<Json<object>>)tProperty);
                        reader.PopulateObject(childObj);
                    } else if (tProperty is TObjArr) {
                        arr = obj.Get((ArrSchema<Json<object>>)tProperty);
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