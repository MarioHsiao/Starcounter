using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Starcounter.Poleposition.Framework;
using System.Diagnostics;
using Starcounter.Poleposition.Util;

namespace Starcounter.Poleposition.Internal
{
/// <summary>
/// A registry of all <see cref="Driver"/>s in a collection of types (e.g., an assembly).
/// To be a valid driver implementation, the class must:
/// <list type="number">
///     <item>inherit <see cref="Driver"/>;</item>
///     <item>declare a <see cref="DriverAttribute"/> with the driver's human readable name;</item>
///     <item>and declare a public constructor taking a <see cref="Setup"/> object.</item>
/// </list>
/// </summary>
public class DriverRegistry
{
    private readonly Dictionary<string, DriverInfo> driversByName =
        new Dictionary<string, DriverInfo>();

    private DriverRegistry(Assembly assembly)
    {
        foreach (var driver in DriverInfo.Extract(assembly.GetTypes()))
        {
            driversByName.Add(driver.Name, driver);
        }
    }

    public static DriverRegistry OfAssembly(Assembly assembly)
    {
        return new DriverRegistry(assembly);
    }


    public IDriverProxy Instantiate(string driverName, Setup setup)
    {
        DriverInfo driver;
        if (!driversByName.TryGetValue(driverName, out driver))
        {
            throw new ArgumentException("No such driver: " + driverName);
        }
        return driver.InstantiateProxy(setup);
    }

    private static bool IsDriverImplementation(Type type)
    {
        return typeof(Driver).IsAssignableFrom(type) && !type.IsAbstract;
    }

    private sealed class LapInfo
    {
        private readonly string name;
        public string Name
        {
            get
            {
                return name;
            }
        }

        private readonly MethodInfo method;
        public MethodInfo Method
        {
            get
            {
                return method;
            }
        }

        private LapInfo(string name, MethodInfo method)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (method == null)
            {
                throw new ArgumentNullException("method");
            }
            this.name = name;
            this.method = method;
        }

        public static IEnumerable<LapInfo> Extract(Type type)
        {
            Debug.Assert(IsDriverImplementation(type));
            foreach (var method in type.GetMethods())
            {
                var lapAttr = Attributes.Find<LapAttribute>(method, true);
                if (lapAttr == null)
                {
                    continue;  // not a lap
                }
                ValidateLapMethod(type, method, lapAttr);
                yield return new LapInfo(lapAttr.Name, method);
            }
        }

        private static void ValidateLapMethod(Type type, MethodInfo method, LapAttribute lapAttr)
        {
            if (method.GetParameters().Length > 0)
            {
                var msg = string.Format(
                              "Lap {0} must not take arguments (type {1}, method {2})",
                              lapAttr.Name, type.FullName, method.Name);
                throw new PolePositionException(msg);
            }
        }

    }

    private sealed class DriverInfo
    {
        private readonly Dictionary<string, LapInfo> lapsByName =
            new Dictionary<string, LapInfo>();

        private readonly string name;
        public string Name
        {
            get
            {
                return name;
            }
        }

        private readonly Type type;
        public Type Type
        {
            get
            {
                return type;
            }
        }


        private DriverInfo(string name, Type type)
        {
            if (name == null)
            {
                throw new ArgumentNullException("name");
            }
            if (type == null)
            {
                throw new ArgumentNullException("type");
            }
            this.name = name;
            this.type = type;
            foreach (var lap in LapInfo.Extract(this.type))
            {
                lapsByName.Add(lap.Name, lap);
            }
        }

        public static IEnumerable<DriverInfo> Extract(IEnumerable<Type> types)
        {
            foreach (var type in types)
            {
                if (!IsDriverImplementation(type))
                {
                    continue;
                }
                var attr = ValidateTypeAndExtractAttr(type);
                yield return new DriverInfo(attr.DriverName, type);
            }
        }

        private static DriverAttribute ValidateTypeAndExtractAttr(Type type)
        {
            if (GetDriverCtor(type) == null)
            {
                var msg = string.Format("Driver type {0} has no Setup constructor", type.FullName);
                throw new PolePositionException(msg);
            }
            var attr = Attributes.Find<DriverAttribute>(type, false);
            if (attr == null)
            {
                var msg = string.Format("Driver type {0} has no {1}",
                                        type.FullName, typeof(DriverAttribute).FullName);
                throw new PolePositionException(msg);
            }
            return attr;
        }

        public IDriverProxy InstantiateProxy(Setup setup)
        {
            return new DriverProxy(this, setup);
        }

        internal Driver InstantiateSubject(Setup setup)
        {
            var ctor = GetDriverCtor(Type);
            return (Driver)ctor.Invoke(new object[] { setup });
        }

        private static ConstructorInfo GetDriverCtor(Type type)
        {
            return type.GetConstructor(new Type[] { typeof(Setup) });
        }

        private sealed class DriverProxy : IDriverProxy
        {
            private readonly DriverInfo driverTemplate;
            private readonly Driver subject;

            public DriverProxy(DriverInfo driverTemplate, Setup setup)
            {
                if (driverTemplate == null)
                {
                    throw new ArgumentNullException("template");
                }
                this.driverTemplate = driverTemplate;
                subject = this.driverTemplate.InstantiateSubject(setup);
            }


            #region IDriverProxy Members

            public void RunLap(string lapName)
            {
                LapInfo lap;
                if (!driverTemplate.lapsByName.TryGetValue(lapName, out lap))
                {
                    var msg = string.Format("No such lap for driver {0}: {1}",
                                            driverTemplate.Name, lapName);
                    throw new ArgumentException(msg);
                }
                lap.Method.Invoke(subject, new object[0]);
            }

            #endregion
        }
    }

}
}
