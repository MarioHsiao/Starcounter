﻿// Generated code. This code matches, parses and invokes Http handlers. The code was generated by the Starcounter http/spdy handler engine.

using Starcounter;
using Starcounter.Internal;
using Starcounter.Internal.Uri;
using System.Text;
using System.Collections.Generic;
using System;
using HttpStructs;

namespace __urimatcher__ {

   public class GeneratedRequestProcessor : TopLevelRequestProcessor {

      public static int Sub0VerificationOffset = 0;
      public static int Sub1VerificationOffset = 6;
      public static int Sub2VerificationOffset = 19;
      public static int Sub4VerificationOffset = 32;
      public static int Sub3VerificationOffset = 47;
      public static int Sub5VerificationOffset = 60;
      public static int Sub6VerificationOffset = 73;
      public static int Sub7VerificationOffset = 88;
      public static int Sub8VerificationOffset = 102;
      public static byte[] VerificationBytes = new byte[] {(byte)'G',(byte)'E',(byte)'T',(byte)' ',(byte)'/',(byte)' ',(byte)'G',(byte)'E',(byte)'T',(byte)' ',(byte)'/',(byte)'p',(byte)'l',(byte)'a',(byte)'y',(byte)'e',(byte)'r',(byte)'s',(byte)' ',(byte)'G',(byte)'E',(byte)'T',(byte)' ',(byte)'/',(byte)'p',(byte)'l',(byte)'a',(byte)'y',(byte)'e',(byte)'r',(byte)'s',(byte)'/',(byte)'G',(byte)'E',(byte)'T',(byte)' ',(byte)'/',(byte)'d',(byte)'a',(byte)'s',(byte)'h',(byte)'b',(byte)'o',(byte)'a',(byte)'r',(byte)'d',(byte)'/',(byte)'G',(byte)'E',(byte)'T',(byte)' ',(byte)'/',(byte)'p',(byte)'l',(byte)'a',(byte)'y',(byte)'e',(byte)'r',(byte)'s',(byte)'?',(byte)'P',(byte)'U',(byte)'T',(byte)' ',(byte)'/',(byte)'p',(byte)'l',(byte)'a',(byte)'y',(byte)'e',(byte)'r',(byte)'s',(byte)'/',(byte)'P',(byte)'O',(byte)'S',(byte)'T',(byte)' ',(byte)'/',(byte)'t',(byte)'r',(byte)'a',(byte)'n',(byte)'s',(byte)'f',(byte)'e',(byte)'r',(byte)'?',(byte)'P',(byte)'O',(byte)'S',(byte)'T',(byte)' ',(byte)'/',(byte)'d',(byte)'e',(byte)'p',(byte)'o',(byte)'s',(byte)'i',(byte)'t',(byte)'?',(byte)'D',(byte)'E',(byte)'L',(byte)'E',(byte)'T',(byte)'E',(byte)' ',(byte)'/',(byte)'a',(byte)'l',(byte)'l',(byte)' '};
      public static IntPtr PointerVerificationBytes;

      public static Sub0Processor Sub0 = new Sub0Processor();
      public static Sub1Processor Sub1 = new Sub1Processor();
      public static Sub2Processor Sub2 = new Sub2Processor();
      public static Sub4Processor Sub4 = new Sub4Processor();
      public static Sub3Processor Sub3 = new Sub3Processor();
      public static Sub5Processor Sub5 = new Sub5Processor();
      public static Sub6Processor Sub6 = new Sub6Processor();
      public static Sub7Processor Sub7 = new Sub7Processor();
      public static Sub8Processor Sub8 = new Sub8Processor();

      public GeneratedRequestProcessor() {
          Registrations["GET / "] = Sub0;
          Registrations["GET /players "] = Sub1;
          Registrations["GET /players/@i "] = Sub2;
          Registrations["GET /dashboard/@i "] = Sub4;
          Registrations["GET /players?@s "] = Sub3;
          Registrations["PUT /players/@i "] = Sub5;
          Registrations["POST /transfer?@i "] = Sub6;
          Registrations["POST /deposit?@i "] = Sub7;
          Registrations["DELETE /all "] = Sub8;
          PointerVerificationBytes = BitsAndBytes.Alloc(VerificationBytes.Length); // TODO. Free when program exists
          BitsAndBytes.SlowMemCopy( PointerVerificationBytes, VerificationBytes, (uint)VerificationBytes.Length);
      }

      public override bool Process(IntPtr uri, int uriSize, IntPtr fragment, int size, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
         unsafe {
            byte* pfrag = (byte*)fragment;
            int nextSize = size;
            switch (*pfrag) {
               case (byte)'G':
                  nextSize -= 5;
                  if (nextSize < 0) {
                      handler = null;
                      resource = null;
                      return false;
                  }
                  pfrag += 5;
                  switch (*pfrag) {
                     case (byte)' ':
                        if (Sub0.Process(uri, uriSize, (IntPtr)pfrag, nextSize, invoke, request, out handler, out resource))
                           return true;
                        break;
                     case (byte)'p':
                        nextSize -= 7;

                        if (nextSize == 0) {
                            if (Sub1.Process(uri, uriSize, (IntPtr)pfrag, nextSize, invoke, request, out handler, out resource))
                                return true;
                            break;
                        }

                        if (nextSize < 0) {
                            handler = null;
                            resource = null;
                            return false;
                        }
                        pfrag += 7;
                        switch (*pfrag) {
                           case (byte)' ':
                              if (Sub1.Process(uri, uriSize, (IntPtr)pfrag, nextSize, invoke, request, out handler, out resource))
                                 return true;
                              break;
                           case (byte)'/':
                              nextSize -= 1;
                              if (nextSize < 0) {
                                  break;
                              }
                              pfrag += 1;
                              if (Sub2.Process(uri, uriSize, (IntPtr)pfrag, nextSize, invoke, request, out handler, out resource))
                                 return true;
                              break;
                           case (byte)'?':
                              nextSize -= 1;
                              if (nextSize < 0) {
                                  break;
                              }
                              pfrag += 1;
                              if (Sub3.Process(uri, uriSize, (IntPtr)pfrag, nextSize, invoke, request, out handler, out resource))
                                 return true;
                              break;
                        }
                        break;
                     case (byte)'d':
                        nextSize -= 10;
                        if (nextSize < 0) {
                            break;
                        }
                        pfrag += 10;
                        if (Sub4.Process(uri, uriSize, (IntPtr)pfrag, nextSize, invoke, request, out handler, out resource))
                           return true;
                        break;
                  }
                  break;
               case (byte)'P':
                  nextSize -= 1;
                  if (nextSize < 0) {
                      handler = null;
                      resource = null;
                      return false;
                  }
                  pfrag += 1;
                  switch (*pfrag) {
                     case (byte)'U':
                        nextSize -= 12;
                        if (nextSize < 0) {
                            break;
                        }
                        pfrag += 12;
                        if (Sub5.Process(uri, uriSize, (IntPtr)pfrag, nextSize, invoke, request, out handler, out resource))
                           return true;
                        break;
                     case (byte)'O':
                        nextSize -= 5;
                        if (nextSize < 0) {
                            handler = null;
                            resource = null;
                            return false;
                        }
                        pfrag += 5;
                        switch (*pfrag) {
                           case (byte)'t':
                              nextSize -= 9;
                              if (nextSize < 0) {
                                  break;
                              }
                              pfrag += 9;
                              if (Sub6.Process(uri, uriSize, (IntPtr)pfrag, nextSize, invoke, request, out handler, out resource))
                                 return true;
                              break;
                           case (byte)'d':
                              nextSize -= 8;
                              if (nextSize < 0) {
                                  break;
                              }
                              pfrag += 8;
                              if (Sub7.Process(uri, uriSize, (IntPtr)pfrag, nextSize, invoke, request, out handler, out resource))
                                 return true;
                              break;
                        }
                        break;
                  }
                  break;
               case (byte)'D':
                  if (Sub8.Process(uri, uriSize, (IntPtr)pfrag, nextSize, invoke, request, out handler, out resource))
                     return true;
                  break;
            }
         }
         handler = null;
         resource = null;
         return false;
      }

      public class Sub0Processor : SingleRequestProcessor {
         private unsafe bool Verify(IntPtr uriStart, int uriSize) {
             byte* ptemplate = (byte*)(PointerVerificationBytes + Sub0VerificationOffset);
             byte* puri = (byte*)uriStart;
             int nextSize = uriSize;
             nextSize -= 4;
             if (nextSize<0 || (*(UInt32*)puri) !=  (*(UInt32*)ptemplate) ) {
                 return false;
             }
             puri += 4;
             ptemplate += 4;
             nextSize --;
             if (nextSize<0 || (*puri) != (*ptemplate) ) {
                 return false;
             }
             return true;
         }

         public override bool Process(IntPtr uri, int uriSize, IntPtr fragment, int size, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
            if (Verify(uri, uriSize)) {
               handler = this;
               if (!invoke)
                  resource = null;
               else
                  resource = Code.Invoke();
               return true;
            }
            handler = null;
            resource = null;
            return false;
         }
      }

      public class Sub1Processor : SingleRequestProcessor {
         private unsafe bool Verify(IntPtr uriStart, int uriSize) {
             byte* ptemplate = (byte*)(PointerVerificationBytes + Sub1VerificationOffset);
             byte* puri = (byte*)uriStart;
             int nextSize = uriSize;
             nextSize -= 8;
             if (nextSize<0 || (*(UInt64*)puri) != (*(UInt64*)ptemplate) ) {
                  return false;
             }
             puri += 8;
             ptemplate += 8;
             nextSize -= 4;
             if (nextSize<0 || (*(UInt32*)puri) !=  (*(UInt32*)ptemplate) ) {
                 return false;
             }
             puri += 4;
             ptemplate += 4;
             return true;
         }

         public override bool Process(IntPtr uri, int uriSize, IntPtr fragment, int size, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
            if (Verify(uri, uriSize)) {
               handler = this;
               if (!invoke)
                  resource = null;
               else
                  resource = Code.Invoke();
               return true;
            }
            handler = null;
            resource = null;
            return false;
         }
      }

      public class Sub2Processor : SingleRequestProcessor<int> {
         private unsafe bool Verify(IntPtr uriStart, int uriSize) {
             byte* ptemplate = (byte*)(PointerVerificationBytes + Sub2VerificationOffset);
             byte* puri = (byte*)uriStart;
             int nextSize = uriSize;
             nextSize -= 8;
             if (nextSize<0 || (*(UInt64*)puri) != (*(UInt64*)ptemplate) ) {
                  return false;
             }
             puri += 8;
             ptemplate += 8;
             nextSize -= 4;
             if (nextSize<0 || (*(UInt32*)puri) !=  (*(UInt32*)ptemplate) ) {
                 return false;
             }
             puri += 4;
             ptemplate += 4;
             nextSize --;
             if (nextSize<0 || (*puri) != (*ptemplate) ) {
                 return false;
             }
             return true;
         }

         public override bool Process(IntPtr uri, int uriSize, IntPtr fragment, int size, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
            if (Verify(uri, uriSize)) {
               int val;
               if (ParseUriInt(fragment,size,out val)) {
                  handler = this;
                  if (!invoke)
                     resource = null;
                  else
                     resource = Code.Invoke(val);
                  return true;
               }
            }
            handler = null;
            resource = null;
            return false;
         }
      }

      public class Sub3Processor : SingleRequestProcessor<string> {
         private unsafe bool Verify(IntPtr uriStart, int uriSize) {
             byte* ptemplate = (byte*)(PointerVerificationBytes + Sub3VerificationOffset);
             byte* puri = (byte*)uriStart;
             int nextSize = uriSize;
             nextSize -= 8;
             if (nextSize<0 || (*(UInt64*)puri) != (*(UInt64*)ptemplate) ) {
                  return false;
             }
             puri += 8;
             ptemplate += 8;
             nextSize -= 4;
             if (nextSize<0 || (*(UInt32*)puri) !=  (*(UInt32*)ptemplate) ) {
                 return false;
             }
             puri += 4;
             ptemplate += 4;
             nextSize --;
             if (nextSize<0 || (*puri) != (*ptemplate) ) {
                 return false;
             }
             return true;
         }

         public override bool Process(IntPtr uri, int uriSize, IntPtr fragment, int size, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
            if (Verify(uri, uriSize)) {
               string val;
               if (ParseUriString(fragment,size,out val)) {
                  handler = this;
                  if (!invoke)
                     resource = null;
                  else
                     resource = Code.Invoke(val);
                  return true;
               }
            }
            handler = null;
            resource = null;
            return false;
         }
      }

      public class Sub4Processor : SingleRequestProcessor<int> {
         private unsafe bool Verify(IntPtr uriStart, int uriSize) {
             byte* ptemplate = (byte*)(PointerVerificationBytes + Sub4VerificationOffset);
             byte* puri = (byte*)uriStart;
             int nextSize = uriSize;
             nextSize -= 8;
             if (nextSize<0 || (*(UInt64*)puri) != (*(UInt64*)ptemplate) ) {
                  return false;
             }
             puri += 8;
             ptemplate += 8;
             nextSize -= 4;
             if (nextSize<0 || (*(UInt32*)puri) !=  (*(UInt32*)ptemplate) ) {
                 return false;
             }
             puri += 4;
             ptemplate += 4;
             nextSize -= 2;
             if (nextSize<0 || (*(UInt16*)puri) != (*(UInt16*)ptemplate) ) {
                 return false;
             }
             puri += 2;
             ptemplate += 2;
             nextSize --;
             if (nextSize<0 || (*puri) != (*ptemplate) ) {
                 return false;
             }
             return true;
         }

         public override bool Process(IntPtr uri, int uriSize, IntPtr fragment, int size, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
            if (Verify(uri, uriSize)) {
               int val;
               if (ParseUriInt(fragment,size,out val)) {
                  handler = this;
                  if (!invoke)
                     resource = null;
                  else
                     resource = Code.Invoke(val);
                  return true;
               }
            }
            handler = null;
            resource = null;
            return false;
         }
      }

      public class Sub5Processor : SingleRequestProcessor<int> {
         private unsafe bool Verify(IntPtr uriStart, int uriSize) {
             byte* ptemplate = (byte*)(PointerVerificationBytes + Sub5VerificationOffset);
             byte* puri = (byte*)uriStart;
             int nextSize = uriSize;
             nextSize -= 8;
             if (nextSize<0 || (*(UInt64*)puri) != (*(UInt64*)ptemplate) ) {
                  return false;
             }
             puri += 8;
             ptemplate += 8;
             nextSize -= 4;
             if (nextSize<0 || (*(UInt32*)puri) !=  (*(UInt32*)ptemplate) ) {
                 return false;
             }
             puri += 4;
             ptemplate += 4;
             nextSize --;
             if (nextSize<0 || (*puri) != (*ptemplate) ) {
                 return false;
             }
             return true;
         }

         public override bool Process(IntPtr uri, int uriSize, IntPtr fragment, int size, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
            if (Verify(uri, uriSize)) {
               int val;
               if (ParseUriInt(fragment,size,out val)) {
                  handler = this;
                  if (!invoke)
                     resource = null;
                  else
                     resource = Code.Invoke(val);
                  return true;
               }
            }
            handler = null;
            resource = null;
            return false;
         }
      }

      public class Sub6Processor : SingleRequestProcessor<int> {
         private unsafe bool Verify(IntPtr uriStart, int uriSize) {
             byte* ptemplate = (byte*)(PointerVerificationBytes + Sub6VerificationOffset);
             byte* puri = (byte*)uriStart;
             int nextSize = uriSize;
             nextSize -= 8;
             if (nextSize<0 || (*(UInt64*)puri) != (*(UInt64*)ptemplate) ) {
                  return false;
             }
             puri += 8;
             ptemplate += 8;
             nextSize -= 4;
             if (nextSize<0 || (*(UInt32*)puri) !=  (*(UInt32*)ptemplate) ) {
                 return false;
             }
             puri += 4;
             ptemplate += 4;
             nextSize -= 2;
             if (nextSize<0 || (*(UInt16*)puri) != (*(UInt16*)ptemplate) ) {
                 return false;
             }
             puri += 2;
             ptemplate += 2;
             nextSize --;
             if (nextSize<0 || (*puri) != (*ptemplate) ) {
                 return false;
             }
             return true;
         }

         public override bool Process(IntPtr uri, int uriSize, IntPtr fragment, int size, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
            if (Verify(uri, uriSize)) {
               int val;
               if (ParseUriInt(fragment,size,out val)) {
                  handler = this;
                  if (!invoke)
                     resource = null;
                  else
                     resource = Code.Invoke(val);
                  return true;
               }
            }
            handler = null;
            resource = null;
            return false;
         }
      }

      public class Sub7Processor : SingleRequestProcessor<int> {
         private unsafe bool Verify(IntPtr uriStart, int uriSize) {
             byte* ptemplate = (byte*)(PointerVerificationBytes + Sub7VerificationOffset);
             byte* puri = (byte*)uriStart;
             int nextSize = uriSize;
             nextSize -= 8;
             if (nextSize<0 || (*(UInt64*)puri) != (*(UInt64*)ptemplate) ) {
                  return false;
             }
             puri += 8;
             ptemplate += 8;
             nextSize -= 4;
             if (nextSize<0 || (*(UInt32*)puri) !=  (*(UInt32*)ptemplate) ) {
                 return false;
             }
             puri += 4;
             ptemplate += 4;
             nextSize -= 2;
             if (nextSize<0 || (*(UInt16*)puri) != (*(UInt16*)ptemplate) ) {
                 return false;
             }
             puri += 2;
             ptemplate += 2;
             return true;
         }

         public override bool Process(IntPtr uri, int uriSize, IntPtr fragment, int size, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
            if (Verify(uri, uriSize)) {
               int val;
               if (ParseUriInt(fragment,size,out val)) {
                  handler = this;
                  if (!invoke)
                     resource = null;
                  else
                     resource = Code.Invoke(val);
                  return true;
               }
            }
            handler = null;
            resource = null;
            return false;
         }
      }

      public class Sub8Processor : SingleRequestProcessor {
         private unsafe bool Verify(IntPtr uriStart, int uriSize) {
             byte* ptemplate = (byte*)(PointerVerificationBytes + Sub8VerificationOffset);
             byte* puri = (byte*)uriStart;
             int nextSize = uriSize;
             nextSize -= 8;
             if (nextSize<0 || (*(UInt64*)puri) != (*(UInt64*)ptemplate) ) {
                  return false;
             }
             puri += 8;
             ptemplate += 8;
             nextSize -= 2;
             if (nextSize<0 || (*(UInt16*)puri) != (*(UInt16*)ptemplate) ) {
                 return false;
             }
             puri += 2;
             ptemplate += 2;
             nextSize --;
             if (nextSize<0 || (*puri) != (*ptemplate) ) {
                 return false;
             }
             return true;
         }

         public override bool Process(IntPtr uri, int uriSize, IntPtr fragment, int size, bool invoke, HttpRequest request, out SingleRequestProcessorBase handler, out object resource) {
            if (Verify(uri, uriSize)) {
               handler = this;
               if (!invoke)
                  resource = null;
               else
                  resource = Code.Invoke();
               return true;
            }
            handler = null;
            resource = null;
            return false;
         }
      }
   }
}


