#include <sccoredb.h>
#include <memhelp4.h>
#include <starcounter-host.h>
#include <v8-host.h>

int StarcounterJs_BootstrapV8();
_host* StarcounterJs_BootstrapStarcounter();
int StarcounterJs_InteractiveShell();
int StarcounterJs_ShutdownStarcounter(_host*);


int main(int argc, char* argv[]) 
{
	struct _host *phost;

   wprintf(
		L"%s.\n%s.\n\n",
		L"Starcounter.Js",
		L"Copyright (C) Starcounter AB 2003-2013. All rights reserved"
		);

	phost = StarcounterJs_BootstrapStarcounter();

   if(!phost) {
      wprintf(L"Could not bootstrap Starcounter");
      return 1;
   }

   StarcounterJs_BootstrapV8();
   StarcounterJs_InteractiveShell();
   //RunShell();

   wprintf(L"Shutting down\n");

   StarcounterJs_ShutdownStarcounter(phost);

	return 0;
}


