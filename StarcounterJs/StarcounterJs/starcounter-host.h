

#include <sccoredb.h>
#include <sccoredbg.h>
#include <sccoreerr.h>
#include <sccorelib.h>
#include <sccorelog.h>
#include <memhelp4.h>
#include <coalmine.h>
#include <formaterr.h>


struct _host
{
	uint64_t hmenv;
	uint64_t hlogs;
	void *hsched;
	void *hevent;
};

