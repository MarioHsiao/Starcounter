using FastReflectionLib;
using Newtonsoft.Json;
using Starcounter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Starcounter.Applications.UsageTrackerApp.Import {
    public class ImportManager {

        bool SetProperties = false;

        Dictionary<ulong, ulong> IndexTable = new Dictionary<ulong, ulong>();   // old objectNo, new ObjectNo

        public void Import(string file) {

            if (string.IsNullOrEmpty(file)) {
                throw new ArgumentException(file);
            }

            Console.WriteLine(string.Format("NOTICE: Importing to: {0} ", file));

            this.IndexTable.Clear();

            // Create items (no properties is set)
            this.StartImport(file);

            this.SetProperties = true;

            // Set setProperties and references
            this.StartImport(file);

        }

        void StartImport(string file) {

            using (StreamReader r = new StreamReader(file)) {

                using (JsonTextReader reader = new JsonTextReader(r)) {


                    // jump over None
                    reader.Read();

                    // jump over StartObject
                    reader.Read();

                    // jump over PropertyName "Tables"
                    reader.Read();

                    int tableCnt = 0;

                    // jump over StartArray
                    while (reader.Read()) {

                        if (reader.TokenType == JsonToken.StartObject) {

                            ImportTable(reader);
                            tableCnt++;
                            if (reader.TokenType != JsonToken.EndObject) {
                                throw new InvalidDataException("Table dosent end with an EndObject");
                            }

                        }

                    }

                    if (this.SetProperties) {
                        Console.WriteLine(string.Format("NOTICE: Total imported tables: {0}", tableCnt));
                    }

                }


            }


        }

        void ImportTable(JsonTextReader reader) {

            FastReflectionCaches.ClearAllCaches();
            Utils.ClearCache();

            if (reader.TokenType != JsonToken.StartObject) {
                // Error
                throw new InvalidDataException("Table dosent start with an StartObject");
            }

            // Jump over StartObject
            reader.Read();

            if (reader.TokenType != JsonToken.PropertyName) {
                // Error
                throw new InvalidDataException("Table dosent have a PropertyName");
            }

            if (reader.ValueType != typeof(String)) {
                throw new InvalidDataException("Table name is not a string");
            }

            Dictionary<string, object> dic = new Dictionary<string, object>();
            string tableClassName = reader.Value.ToString();

            // Jump over PropertyName
            reader.Read();

            if (reader.TokenType != JsonToken.StartArray) {
                // Error
                throw new InvalidDataException("Table dosent have have a StartArray object");
            }

            int itemCnt = 0;

            while (reader.Read()) {

                if (reader.TokenType == JsonToken.StartObject) {
                    dic.Clear();
                    continue;
                }

                if (reader.TokenType == JsonToken.EndObject) {
                    OnNewItem(tableClassName, dic);
                    itemCnt++;
                    continue;
                }

                if (reader.TokenType == JsonToken.EndArray) {

                    // Jump over EndArray
                    reader.Read();
                    if (reader.TokenType != JsonToken.EndObject) {
                        // Error
                        throw new InvalidDataException("Table dosent have have a EndObject object");
                    }

                    if (this.SetProperties) {
                        Console.WriteLine(string.Format("NOTICE: Exported table {0} ({1} items)", tableClassName, itemCnt));
                    }
                    return;
                }

                // Add properties
                if (reader.TokenType == JsonToken.PropertyName) {
                    string key = reader.Value.ToString();

                    // Jump to value
                    reader.Read();

                    dic.Add(key, reader.Value);

                }
            }

            throw new InvalidOperationException("Could not find end of object");

        }

        void OnNewItem(string className, Dictionary<string, object> dic) {

            Db.Transaction(() => {

                string classQualifiedName = Assembly.CreateQualifiedName("Starcounter.UsageTracker", className);

                Type classType = Type.GetType(classQualifiedName);

                if (classType == null) {
                    throw new TypeLoadException(string.Format("Failed to get the type of {0}, Is HostApp running?", className));
                }

                ulong orginalObjectNo = this.GetObjectNoFromDictionary(className, dic);


                if (this.SetProperties) {

                    // Get instance
                    ulong newInstanceObjectNo = this.IndexTable[orginalObjectNo];

                    var instance = Db.SlowSQL("SELECT o FROM " + className + " o WHERE o.ObjectNo=?", newInstanceObjectNo).First;
                    if (instance == null) {
                        throw new KeyNotFoundException(string.Format("Can not find ObjectNo={0} in {1}", newInstanceObjectNo, className));
                    }

                    foreach (KeyValuePair<string, object> item in dic) {

                        if (item.Key == "ObjectNo") {
                            continue;
                        }

                        string prefixSuffix = "+REF+";

                        if (item.Value is string && ((string)item.Value).StartsWith(prefixSuffix) && ((string)item.Value).EndsWith(prefixSuffix)) {

                            string value = (string)item.Value;

                            // Remove prefixSuffix
                            value = value.Substring(prefixSuffix.Length, value.Length - (prefixSuffix.Length * 2));

                            string[] values = value.Split(':');

                            string refClassName = values[0];
                            string refPropertyName = values[1];

                            if (!("null".Equals(values[2]))) {

                                ulong refObjectNo = ulong.Parse(values[2]);

                                ulong currentObjectNo = this.IndexTable[refObjectNo];

                                var result = Db.SlowSQL("SELECT o FROM " + refClassName + " o WHERE o.ObjectNo=?", currentObjectNo).First;
                                if (result == null) {
                                    throw new KeyNotFoundException(string.Format("Can not find objectNo={0} in {1}", currentObjectNo, refClassName));
                                }

                                Utils.SetValue(instance, item.Key, result);
                            }

                        }
                        else {

                            Utils.SetValue(instance, item.Key, item.Value);
                        }
                    }

                }
                else {

                    // Create a new instance of the class
                    object instance = Activator.CreateInstance(classType);

                    // Add old and new object no to the dictionary
                    this.IndexTable.Add(orginalObjectNo, instance.GetObjectNo());

                }

            });

        }


        private ulong GetObjectNoFromDictionary(string className, Dictionary<string, object> dic) {

            foreach (KeyValuePair<string, object> item in dic) {

                // Juse create instance and keep track of old and new ObjectNo.
                if (item.Key == "ObjectNo") {
                    return (ulong)((long)item.Value);
                }
            }

            throw new MissingFieldException("ObjectNo in class " + className);

        }


    }



}
