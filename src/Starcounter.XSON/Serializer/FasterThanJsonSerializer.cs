﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Starcounter.Internal;
using Starcounter.Templates;

namespace Starcounter.Advanced.XSON {
	internal class FasterThanJsonSerializer : TypedJsonSerializer {
		private const int MAX_INT_SIZE = 11;

		public override int Serialize(Json obj, out byte[] buffer) {
			bool recreateBuffer;
			byte[] buf;
			byte[] childObjArr;
			int templateNo;
			int posInArray;
			int valueSize;
			List<Template> exposedProperties;
			Json childObj;
			Template tProperty;
			TObject tObj;
			TupleWriterBase64 writer;

			// The following variables are offset for remembering last position when buffer needs to be increased:
			// templateNo: The position in the PropertyList that was about to be written.
			// offset: The last verified position in the buf that was succesfully written.
			// nameWritten: If set to true, the name of the template is succesfully written (but not the value).
			// childObjArr: If last value was an object or an object in an array, the array contains the serialized object.
			// posInArray: Set to the last succesful copied value for an objectarray.

			if (obj.IsArray) {
				throw new NotImplementedException("Serializer does not support arrays as root elements");
			}

			tObj = (TObject)obj.Template;
			buf = new byte[512];
			templateNo = 0;
			childObjArr = null;
			posInArray = -1;
			recreateBuffer = false;
			valueSize = -1;
			writer = new TupleWriterBase64();

			exposedProperties = tObj.Properties.ExposedProperties;
			int valueCount = exposedProperties.Count;
			int offset = 0;

			// TODO:
			// Need some way to figure out how many tables we have since we need to increase valuecount.
			for (int i = 0; i < exposedProperties.Count; i++) {
				if (exposedProperties[i] is TObjArr)
					valueCount++;
			}

			unsafe {
restart:
				if (recreateBuffer) {
					offset = writer.Length;
					buf = IncreaseCapacity(buf, offset, valueSize);
				}
				
				// Starting from the last written position
				fixed (byte* p = &buf[0]) {
					writer = new TupleWriterBase64(p, (uint)valueCount);

					if (recreateBuffer) {
						int initLen = writer.Length;
						writer.HaveWritten((uint)(offset - initLen));
					}
					recreateBuffer = true;

					for (int i = templateNo; i < exposedProperties.Count; i++) {
						tProperty = exposedProperties[i];

						if (tProperty is TObject) {
							if (childObjArr == null) {
								childObj = obj.Get((TObject)tProperty);
								valueSize = ((TContainer)childObj.Template).ToFasterThanJson(childObj, out childObjArr);
							}
							if (valueSize != -1) {
								if (childObjArr != null) {
									if (valueSize > (buf.Length - writer.Length))
										goto restart;

									Buffer.BlockCopy(childObjArr, 0, buf, writer.Length, valueSize);
									writer.HaveWritten((uint)valueSize);
									childObjArr = null;
								}
							} else
								goto restart;
						} else if (tProperty is TObjArr) {
							Arr arr = obj.Get((TObjArr)tProperty);
							if (posInArray == -1) {
								if (MAX_INT_SIZE > (buf.Length - writer.Length))
									goto restart;
								writer.Write((ulong)arr.Count);
								posInArray = 0;
							}

							for (int arrPos = posInArray; arrPos < arr.Count; arrPos++) {
								if (childObjArr == null) {
									valueSize = ((TContainer)arr[arrPos].Template).ToFasterThanJson(arr[arrPos], out childObjArr);
									if (valueSize == -1)
										goto restart;

									if (valueSize > (buf.Length - writer.Length))
										goto restart;
								}

								Buffer.BlockCopy(childObjArr, 0, buf, writer.Length, valueSize);
								writer.HaveWritten((uint)valueSize);
								childObjArr = null;
								posInArray++;
							}
							posInArray = -1;
						} else {
							string valueAsStr;
							ulong valueAsUL;

							if (tProperty is TBool) {
								if (buf.Length < (writer.Length + 1))
									goto restart;

								bool b = obj.Get((TBool)tProperty);
								if (b) writer.Write(1);
								else writer.Write(0);
							} else if (tProperty is TDecimal) {
								valueAsStr = obj.Get((TDecimal)tProperty).ToString("0.0###########################", CultureInfo.InvariantCulture);
								valueSize = valueAsStr.Length;
								if (valueSize > (buf.Length - writer.Length))
									goto restart;
								writer.Write(valueAsStr);
							} else if (tProperty is TDouble) {
								valueAsStr = obj.Get((TDouble)tProperty).ToString("0.0###########################", CultureInfo.InvariantCulture);
								valueSize = valueAsStr.Length;
								if (valueSize > (buf.Length - writer.Length))
									goto restart;
								writer.Write(valueAsStr);
							} else if (tProperty is TLong) {
								valueAsUL = (ulong)obj.Get((TLong)tProperty);
								valueSize = MAX_INT_SIZE;
								if (valueSize > (buf.Length - writer.Length))
									goto restart;
								writer.Write(valueAsUL);
							} else if (tProperty is TString) {
								valueAsStr = obj.Get((TString)tProperty);
								if (valueAsStr == null)
									throw new NotImplementedException("null values are not yet supported");
								valueSize = valueAsStr.Length;
								if (valueSize > (buf.Length - writer.Length))
									goto restart;
								writer.Write(valueAsStr);
							} else if (tProperty is TTrigger) {
								throw new NotImplementedException("null values are not yet supported");
//								valueSize = JsonHelper.WriteNull((IntPtr)pfrag, buf.Length - offset);
							}

						}
						templateNo++;
					}
					offset = (int)writer.SealTuple();
				}
			}

			buffer = buf;
			return offset;
		}

		public override int Populate(Json obj, IntPtr src, int srcSize) {
			if (obj.IsArray) {
				throw new NotImplementedException("Cannot serialize JSON where the root object is an array");
			}

			unsafe {
				List<Template> exposedProperties = ((TObject)obj.Template).Properties.ExposedProperties;
				int valueCount = exposedProperties.Count;

				// TODO:
				// Need some way to figure out how many tables we have since we need to increase valuecount.
				for (int i = 0; i < exposedProperties.Count; i++) {
					if (exposedProperties[i] is TObjArr)
						valueCount++;
				}

				var reader = new TupleReaderBase64((byte*)src, (uint)valueCount);

				Arr arr;
				Json childObj;
				TObject tObj = (TObject)obj.Template;
				Template tProperty;
				String valueAsStr;

				for (int i = 0; i < exposedProperties.Count; i++){
					tProperty = exposedProperties[i];

					try {
						if (tProperty is TBool) {
							ulong v = reader.ReadUInt();
							if (v == 1) obj.Set((TBool)tProperty, true);
							else obj.Set((TBool)tProperty, false);
						} else if (tProperty is TDecimal) {
							valueAsStr = reader.ReadString();
							obj.Set((TDecimal)tProperty, Decimal.Parse(valueAsStr, CultureInfo.InvariantCulture));
						} else if (tProperty is TDouble) {
							valueAsStr = reader.ReadString();
							obj.Set((TDouble)tProperty, Double.Parse(valueAsStr, CultureInfo.InvariantCulture));
						} else if (tProperty is TLong) {
							obj.Set((TLong)tProperty, (long)reader.ReadUInt());
						} else if (tProperty is TString) {
							obj.Set((TString)tProperty, reader.ReadString());
						} else if (tProperty is TObject) {
							childObj = obj.Get((TObject)tProperty);
							((TContainer)childObj.Template).PopulateFromFasterThanJson(childObj, (IntPtr)reader.AtEnd, 0);
							reader.Skip();
						} else if (tProperty is TObjArr) {
							arr = obj.Get((TObjArr)tProperty);
							int arrItemCount = (int)reader.ReadUInt();
							for (int aic = 0; aic < arrItemCount; aic++) {
								childObj = arr.Add();
								((TContainer)childObj.Template).PopulateFromFasterThanJson(childObj, (IntPtr)reader.AtEnd, 0);
								reader.Skip();
							}
						}
					} catch (InvalidCastException ex) {
						JsonHelper.ThrowWrongValueTypeException(ex, tProperty.TemplateName, tProperty.JsonType, reader.ReadString());
					}
				}
				return (int)reader.ReadByteCount;
			}
		}

		protected static byte[] IncreaseCapacity(byte[] current, int offset, int needed) {
			byte[] tmpBuffer;
			long bufferSize = current.Length;

			bufferSize *= 2;
			if (needed != -1) {
				while (bufferSize < (offset + needed))
					bufferSize *= 2;
			}
			//            System.Diagnostics.Debug.WriteLine("Increasing buffer, new size: " + bufferSize);
			tmpBuffer = new byte[bufferSize];
			Buffer.BlockCopy(current, 0, tmpBuffer, 0, offset);
			return tmpBuffer;
		}
	}
}
