using Starcounter;
using System;
using System.Diagnostics;

namespace QueryProcessingTest {
    public static class TestKinds {
        public static void RunKindsTest() {
            HelpMethods.LogEvent("Test kinds like behavior");
            Db.Transaction(delegate {
                var porsche911 = new CarModel() { Brand = "Porsche", ModelName = "911" };
                var porsche911turbo = new CarModel() {
                    Brand = "Porsche",
                    ModelName = "911 Turbo",
                    BasedOn = porsche911
                };
                Trace.Assert(porsche911.ModelName == "911");
                var erik = new Person() { Name = "Erik Ohlsson" };
                var jocke = new Person() { Name = "Joachim Wester" };
                var myCar = new Car() { Model = porsche911turbo, Owner = jocke, LicensePlate = "GGU 567" };
                var yourCar = new Car() { Model = porsche911, Owner = erik, LicensePlate = "ABC 123" };
            Console.WriteLine("Print all porsche 911");
            foreach (Car c in Db.SQL<Car>("SELECT c FROM Car C WHERE c IS ?", porsche911))
                Console.WriteLine(c.LicensePlate);
            Console.WriteLine("Print all porsche 911 turbo");
#if false // This code is not possible due to Prolog parser limitation
      foreach (Car c in Db.SQL<Car>("SELECT c FROM Car C WHERE c IS '911 Turbo'"))
            Console.WriteLine(c.LicensePlate);
#endif
            foreach (Car c in Db.SQL<Car>("SELECT c FROM Car C WHERE c IS ?", porsche911turbo))
                Console.WriteLine(c.LicensePlate);
            });
            HelpMethods.LogEvent("Finished testing kinds like behavior");
        }
    }

    [Database]
    public class Person {
        public string Name;
    }

    [Database]
    public class Car {
        public string LicensePlate;
        public Person Owner;
        [Type]
        public CarModel Model;
    }

    [Database]
    public class CarModel {
        public string Brand; // I.e. Porsche
        [TypeName]
        public string ModelName; // I.e. 911
        [Inherits]
        public CarModel BasedOn;
    }
}
