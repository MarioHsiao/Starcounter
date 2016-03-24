
[Company_json]
partial class Company : Json {

    void Handle( Input.CompanyName input ) {
    }

    void Handle(Input.Contact.FirstName input) {
    }

}

[Company_json.Contact]
partial class ContactJson : Json {

    void Handle(Input.FirstName input) {
    }

    void Handle(Input.LastName input) {
    }

}

