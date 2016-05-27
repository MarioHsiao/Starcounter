using System;

namespace Starcounter.XSON.CodeGeneration.Tests {
    partial class databound : Json<Person> {
        [databound_json.SingleNumber]
        partial class PhoneNumberJson : Json<PhoneNumber> {
        }
    }
}
