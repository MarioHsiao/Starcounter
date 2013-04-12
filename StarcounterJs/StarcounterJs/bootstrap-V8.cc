

#include <assert.h>
#include <fcntl.h>
#include <string.h>
#include <stdio.h>
#include <stdlib.h>

#include <v8-host.h>

v8::Persistent<v8::Context> TheV8Context;
v8::Isolate* TheV8Isolate;
v8::Persistent<v8::ObjectTemplate> TheGlobal;


void AddShellFunctions();



int StarcounterJs_ShutdownV8() {
  return 0;
}



//     	if (!r) WaitForSingleObject(phost->hevent, -1);


int StarcounterJs_BootstrapV8() {
// v8::V8::SetFlagsFromCommandLine(&argc, argv, true);

     v8::Isolate* isolate = v8::Isolate::New();
     isolate->Enter();
     TheV8Isolate = isolate;
    //v8::HandleScope handle_scope(isolate);

     v8::HandleScope handle_scope(isolate);
     TheGlobal = v8::Persistent<v8::ObjectTemplate>::New(isolate,v8::ObjectTemplate::New());


     AddShellFunctions();
     TheV8Context = v8::Context::New(NULL, TheGlobal);

    if (TheV8Context.IsEmpty()) {
      fprintf(stderr, "Error creating context\n");
      return 1;
    }
    TheV8Context->Enter();

//    result = RunMain(isolate);
  //  RunShell();
//    context->Exit();
//    context.Dispose(isolate);
//  v8::V8::Dispose();
//  return result;
  return 0;
}

