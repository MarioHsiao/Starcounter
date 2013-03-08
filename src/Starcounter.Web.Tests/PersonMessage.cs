using Starcounter.Templates;
using System;

namespace Starcounter.Internal.Tests {
    public class PersonMessage : Message {
        private static TMessage Schema;

        static PersonMessage() {
            Schema = new TMessage() { ClassName = "PersonMessage", InstanceType = typeof(PersonMessage) };
            Schema.Add<TString>("FirstName");
            Schema.Add<TString>("LastName");
            Schema.Add<TLong>("Age");

            var phoneNumber = new TMessage();
            phoneNumber.Add<TString>("Number");
            Schema.Add<TArr<Message, TMessage>>("PhoneNumbers", phoneNumber);
        }

        public PersonMessage() {
            Template = Schema;
        }
    }
}
