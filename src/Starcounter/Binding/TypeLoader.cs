
using System;
using System.Reflection;

namespace Starcounter.Binding
{
    
    public class TypeLoader
    {

        private readonly string assemblyString_;
        private readonly string typeName_;

        public TypeLoader(string assemblyString, string typeName)
        {
            assemblyString_ = assemblyString;
            typeName_ = typeName;
        }

        public Type Load()
        {
            Assembly a = Assembly.LoadFile(assemblyString_);
            Type t = a.GetType(typeName_);
            return t;
        }
    }
}
