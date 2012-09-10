
using System;
using System.Reflection;

namespace Starcounter.Binding
{
    
    public class TypeLoader
    {

        private readonly AssemblyName assemblyName_;
        private readonly string typeName_;

        public TypeLoader(AssemblyName assemblyName, string typeName)
        {
            assemblyName_ = assemblyName;
            typeName_ = typeName;
        }

        public Type Load()
        {
            Assembly a = Assembly.Load(assemblyName_);
            Type t = a.GetType(typeName_, true);
            return t;
        }
    }
}
