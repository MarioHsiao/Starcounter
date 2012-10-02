

Goals
===========================
1. The solution needs to consume few clock cycles, and low memory
2. The solution needs to mimic a simple generic POCO/POJO JSON tree functionality


Design
===========================
1. Left to right writing of entire tree in a single blob
2. Whenever the blob runs out of space, allocate a new blob twice as big
3. Whenever a value is enlarged inside a blob, create a new referenced blob
4. Use raw inlined APIs to write tree to save clock cycles.
5. Support four encodings:
   a) 'T' Behaved text. Fallback. Slow but client friendly. Text format not allowing control characters
   b) 'P' Printable ASCII. Default for clients not allowing binary communication. Text format allowing control characters
   c) 'B' Binary. Default option. Binary format using only UTF8 for strings
   d) 'X'  Raw. Super fast. Very client unfriendly. Binary format allowing TurboText strings for database bound attributes



Tuples can only be written from left to right.
When updated such that a value is bigger: The host tuple changes to a reference to a blob and a new blob is created.
Nested tuple needs to be completed before the next tuple value can be written.




PrintableUtf8Byte Encoding; // 'T' means UTF-8 text using base64 notation of integers (including lengths and offsets). Addressing uses TxtVarInt32. Floating point and decimal values uses Base64 based structures (see below). 
                            // Future: '&' means same binary format for primitives as Google protocol buffers
I=Insert
D=Delete
U=Update

TxtVarInt32 comes in three lengths:
===================================
* First byte in "@ABCDEFGHIJKLMNO" uses one byte and ranges from 4 bit
* First byte in "`abcdefghijklmno" uses two bytes. Second byte uses Base64. 10 bit (4+6)
* First byte in "0123" uses six bytes. Five bytes uses Base64. 32 bit (2+6+6+6+6+6)



Null strings are supported
===========================
string containing '\0' means null

Integer should add 1 or -1
===========================
Integer 0 means null
Integer 1 means 0
Integer -1 means 0


struct Decimal
{
    unsigned Base64x1 Size;
	unsinged Base64x1 Decimals;
	Base64xSize Integer;
}

struct Integer
{
    unsigned Base64x1 Size;
	Base64xSize Integer;
}

A tuple is always big enough to allow for a '@' external reference

FriendlyTuple // Version one only supports text with decimal numbers. Not really cpu friendly. Not bandwidth optimal.
{
   Byte ExternalFlagAndSizeOfEachOffsetInteger;  // '@' means that this is an external blob reference.
                                                              // 'A' means that each offset is one byte
												        	  // 'B' means that each offset is two bytes
												        	  // 'C' means that each offset is three bytes
												        	  // 'D' means that each offset is four bytes
													          // 'E' means that each offset is five bytes 
   Array< int_SizeOfEachOffsetInteger >[ elementsInSchema-1+1 ] Offsets;
   Array<Value>[ elementsInSchema ] Values;
}

Value
{
    Data union
    {
       FriendlyTuple<Table> Table,
       FriendlyTuple<List> List,
       Array<Utf8Char> Text,
       Array<Utf8Char> FriendlyTupleReference
    };
}

A table is a tuple with the first value describing the number of elements and the other values are the tuples representing the rows.
A list is a tuple with the first value telling the DocumentClassReference and the second value describing the number of elements and the other values are the tuples representing the rows.

