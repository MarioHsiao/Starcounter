
using Starcounter;

partial class Master : Json {
	[Master.json.Emails]
	partial class EmailsObj : Json<Email> {

        void Handle(Input.Emails.Title input)
        {
            this.Transaction.Commit();
        }
	}
}
