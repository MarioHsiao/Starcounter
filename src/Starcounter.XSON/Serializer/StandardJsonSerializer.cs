
using System;
using System.Runtime.InteropServices;
using System.Text;
using Starcounter.Internal;
using Starcounter.Templates;

namespace Starcounter.Advanced.XSON {

    public class StandardJsonSerializer : StandardJsonSerializerBase {
        public override int Populate(Json obj, IntPtr source, int sourceSize) {
            string propertyName;

            if (obj.IsArray) {
                throw new NotImplementedException("Cannot serialize JSON where the root object is an array");
            }

            var reader = new JsonReader(source, sourceSize);
            Arr arr;
            Json childObj;
            TObject tObj = (TObject)obj.Template;
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
                    else if (tProperty is TObject) {
                        childObj = obj.Get((TObject)tProperty);
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