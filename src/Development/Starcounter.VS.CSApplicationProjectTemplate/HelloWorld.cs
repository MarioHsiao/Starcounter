using Starcounter;

namespace $safeprojectname$ {
    class Hello {
        static void Main() {
            Db.Transaction( () => {
                var albert = new Person() { FirstName="Albert", LastName="Einstein" };
                new Quote() { Person=albert, Text="Make things as simple as possible, but not simpler" };
            });
 
            Handle.GET("/hello/{?}", (string firstName) => {
                Quote q = Db.SQL("SELECT Q FROM Quote Q WHERE Person.FirstName=?", firstName ).First;
                return "<!DOCTYPE html><title>My first app</title>" + q.Person.FirstName + " " + 
                        q.Person.LastName + " says: " + q.Text;
            });
        }
    }
 
    [Database]
    public class Person {
        public string FirstName;
        public string LastName;
    }
 
    [Database]
    public class Quote {
        public Person Person;
        public string Text;
    }
}