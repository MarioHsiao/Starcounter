
using Starcounter;

partial class Master : Json {
	[Master.json.Emails]
	partial class EmailsObj : Json<Email> {
	}
}
