using Starcounter;
using System;

class Program {
	static void Main() {
		Db.Transact(delegate {
			new Organization {
				Name = "CoolCompany",
				OrganizationNr = 125301 
			};
			new Organization {
				Name = "My AB",
				OrganizationNr = 530284
			};
		});
	}
}

[Database]
public class Organization {
	public string Name;
	public uint OrganizationNr;
}