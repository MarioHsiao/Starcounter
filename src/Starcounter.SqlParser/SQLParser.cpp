// SQLParser.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

/// <summary>Initialize memory and error management into thread global. Has to be called before parsing.
/// </summary>
void InitParser()
{
	MemoryContextInit(32 * 1024);
	InitScError();
}

/// <summary>Resets memory management that the same allocated memory in Init can be reused.
/// </summary>
void ResetMemoryContext()
{
	MemoryContextDeleteChildren(TopMemoryContext);
	MemoryContextReset(TopMemoryContext);
}

///<summary>Removes allocated memory.
///</summary>
bool CleanMemoryContext()
{
	ResetMemoryContext();
	AllocSetDelete(TopMemoryContext);
	scerror = NULL;
	free(TopMemoryContext);
	TopMemoryContext = NULL;
	return true;
}

///<summary>Check if memory leaks. First it cleans memory if necessary, then dumps leaks if such.
///</summary>
///<returns>Returns true if a leak is found, false if no leaks are found.
///</returns>
bool DumpMemoryLeaks()
{
	if (TopMemoryContext != NULL)
	{
		CleanMemoryContext();
	}
	_CrtSetReportMode( _CRT_WARN, _CRTDBG_MODE_FILE );
	_CrtSetReportFile( _CRT_WARN, _CRTDBG_FILE_STDOUT );
	return (_CrtDumpMemoryLeaks() != 0);
}


///<summary>Main method for parsing. It initializes memory if necessary, calls C parser, reports errors if happened during parsing.
///Errors are reported to thread global variable and in some cases exception is thrown in cases where  bison error management cannot be used.
///</summary>
///<param name="query">Query string to parse.</param>
///<param name="errorcode">Used to return error code. 0 if no error produced. More error information obtained from another function</param>
///<returns>Returns list of raw parsed trees. Usually list contains single tree, since query string contains one query.</returns>
List *ParseQuery(const wchar_t *query, int *errorcode)
{
	List *parsedTree = NULL;
	if (TopMemoryContext == NULL)
		InitParser();
	else
	{
		ResetMemoryContext();
		ResetScError();
	}
	try
	{
		Size qlen = wcslen(query)*sizeof(wchar_t);
		parsedTree = raw_parser(query);
	}
	catch (UnmanagedParserException& e)
	{
		if (scerror->scerrorcode == 0)
			scerror->scerrorcode = e.error_code();
	}
	*errorcode = scerror->scerrorcode;
	return parsedTree;
}

///<returns>Returns pointer to structure with error information such as location, tocken and message
///</returns>
ScError *GetScError()
{
	return scerror;
}

char* TreeToString(List *tree)
{
	return nodeToString(tree);
}

char* ParseQueryToString(const wchar_t *str)
{
	int errorcode = 0;
	List *parseTree = ParseQuery(str, &errorcode);
	if (parseTree && !errorcode)
		return TreeToString(parseTree);
	else
		return "\nError: No tree created or error happened!\n";
}

/*
 * Interface functions to help functions of C parser.
*/
wchar_t* StrVal(Node *val)
{
	return strVal(val);
}
int Location(Node *node)
{
	return exprLocation(node);
}
List* LAppend(List *flist, Node *slist)
{
	return lappend(flist, slist);
}
List* LCons(Node *flist, List *slist)
{
	return lcons(flist, slist);
}


//int main(void)
//{
//	int errorcode = 0;
//	printf(TreeToString(ParseQuery("select * from tbl", &errorcode)));
//	CleanMemoryContext();
//	DumpMemoryLeaks();
//	return 0;
//}

//int _tmain(int argc, TCHAR* argv[])
//{
//	int i = 0;
//	printf("Input: ");
//	for (i = 0; i < argc; i++)
//		printf("\nargument %d: %s", i, argv[i]);
//	printf("\n-----\n");
//	MemoryContextInit();
//	for (i = 1; i < argc; i++)
//	{
//		printf("\nParsing argument %d: %s", i, argv[i]);
//		MyPrintNodes(argv[i]);
//	}
//	printf("\nParsing is completed!");
//	return 0;
//}
