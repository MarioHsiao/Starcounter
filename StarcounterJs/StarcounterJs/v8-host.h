
#ifndef V8HOST_H
#define V8HOST_H

#include <v8.h>

extern v8::Persistent<v8::Context> TheV8Context;
extern v8::Isolate* TheV8Isolate;
extern v8::Persistent<v8::ObjectTemplate> TheGlobal;

#endif