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
                var justCar = new Car() { LicensePlate = "TSL 430" };
                String[] resPlates = { "GGU 567", "ABC 123" };
                int count = 0;
                foreach (Car c in Db.SQL<Car>("SELECT c FROM Car C WHERE c IS ?", porsche911)) {
                    Trace.Assert(resPlates[count] == c.LicensePlate);
                    count++;
                }
                Trace.Assert(resPlates.Length == count);
#if false // This code is not possible due to Prolog parser limitation
      foreach (Car c in Db.SQL<Car>("SELECT c FROM Car C WHERE c IS '911 Turbo'"))
            Console.WriteLine(c.LicensePlate);
#endif
                count = 0;
                foreach (Car c in Db.SQL<Car>("SELECT c FROM Car C WHERE c IS ?", porsche911turbo)) {
                    Trace.Assert(c.LicensePlate == resPlates[count]);
                    count++;
                }
                Trace.Assert(count == 1);
                Trace.Assert(Db.SQL<Car>("select c from car c where c is ? and owner = ?", porsche911turbo, erik).First == null);
                Trace.Assert(Db.SQL<Car>("select c from car c where owner = ? and c is ?", erik, porsche911turbo).First == null);
                count = 0;
                foreach (Car c in Db.SQL<Car>("select c from car c where owner = ? and c is ?", jocke, porsche911)) {
                    Trace.Assert(c.LicensePlate == resPlates[count]);
                    count++;
                }
                Trace.Assert(count == 1);
                Trace.Assert(Db.SQL("select p from person p where p is ?", porsche911).First == null);
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
