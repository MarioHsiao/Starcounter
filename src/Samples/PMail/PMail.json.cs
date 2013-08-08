using Starcounter;

partial class PMail : Json {
    [PMail.json.Mailboxes] /*is this needed? I discovered it only works with this - Marcin*/
    partial class MailboxesObj : Json<Mailbox> {
    }

	[PMail.json.Mails]
	partial class MailsObj : Json<Mail> {
	}
}
