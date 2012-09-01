// STARTING STATIC EXTERNAL CODE SECTION.

#define MAX_KEY_SIZE 1024
#define DLL_EXPORT extern "C" __declspec(dllexport)
#define INTERNAL_FUNCTION inline static
#define INTERNAL_DATA static
#define CALL_CONV __stdcall
#define MDBIT_OBJECTID 0
#define SC_ITERATOR_RANGE_VALID_LSKEY 0x00000001
#define SC_ITERATOR_RANGE_INCLUDE_LSKEY 0x00000010
#define SC_ITERATOR_RANGE_VALID_GRKEY 0x00000002
#define SC_ITERATOR_RANGE_INCLUDE_GRKEY 0x00000020
#define SC_ITERATOR_SORTED_DESCENDING 0x00080000

#define Mdb_DataValueFlag_Exceptional 0x1030
#define Mdb_DataValueFlag_Null 0x0001

// Unique query ID.
#define UNIQUE_ID REPLACE_ME_ID

// Data types definitions.
typedef unsigned long long UINT64;
typedef long long INT64;
typedef unsigned int UINT32;
typedef int INT32;
typedef unsigned short UINT16;
typedef short INT16;
typedef unsigned char UINT8;
typedef char INT8;

typedef char CHAR;
typedef wchar_t WCHAR;
typedef bool BOOL;
typedef void VOID;
typedef float FLOAT;
typedef double DOUBLE;

// Function pointers into Blue.
typedef UINT32 (*SCIteratorCreate_Type) (UINT64 hIndex, UINT32 flags, const UINT8 *firstKey, const UINT8 *lastKey, UINT64 *ph, UINT64 *pv);
static SCIteratorCreate_Type SCIteratorCreate = 0;

typedef UINT32 (*SCIteratorNext_Type) (UINT64 h, UINT64 v, UINT64* pObjectOID, UINT64* pObjectETI, UINT16* pClassIndex, UINT64* pCargo);
static SCIteratorNext_Type SCIteratorNext = 0;

typedef UINT32 (*SCIteratorFree_Type) (UINT64 h, UINT64 v);
static SCIteratorFree_Type SCIteratorFree = 0;

typedef UINT16 (*Mdb_ObjectReadInt64_Type)(UINT64 objectOID, UINT64 objectETI, INT32 index, INT64* pReturnValue);
static Mdb_ObjectReadInt64_Type Mdb_ObjectReadInt64 = 0;

// Blue function pointers container.
struct BlueFunctionPointers
{
    SCIteratorCreate_Type pSCIteratorCreate;
    SCIteratorNext_Type pSCIteratorNext;
    SCIteratorFree_Type pSCIteratorFree;
    Mdb_ObjectReadInt64_Type pMdb_ObjectReadInt64;
};

// Blue function pointers transfer.
DLL_EXPORT VOID CALL_CONV InitBlueFunctions_REPLACE_ME_ID(BlueFunctionPointers *blueFunctionPointers)
{
    SCIteratorCreate = (blueFunctionPointers->pSCIteratorCreate);
    SCIteratorNext = (blueFunctionPointers->pSCIteratorNext);
    SCIteratorFree = (blueFunctionPointers->pSCIteratorFree);
    Mdb_ObjectReadInt64 = (blueFunctionPointers->pMdb_ObjectReadInt64);
}

// Entry point function.
DLL_EXPORT INT32 CALL_CONV JustEntryPoint_REPLACE_ME_ID()
{
    return 1;
}

// Verification function.
DLL_EXPORT UINT64 CALL_CONV VerifyMe_REPLACE_ME_ID()
{
    return UNIQUE_ID;
}

// Query parameters packaged in byte array.
INTERNAL_DATA UINT8 *g_QueryParamsData = 0;

// Template for specific range calculation function.
struct ScanRange;
typedef VOID (*CalculateRange_Type) (ScanRange *range);

// Scan range representation.
struct ScanRange
{
    UINT8 first[MAX_KEY_SIZE]; // First range point.
    UINT8 second[MAX_KEY_SIZE]; // Second range point.

    UINT32 firstPos; // Offset for the first range point.
    UINT32 secondPos; // Offset for the second range point.

    UINT32 flags; // Indicating the range validity.
    BOOL isEquality; // Indicating the equality range.

    CalculateRange_Type CalculateRange; // Function pointer to the specific range calculation function.

    ScanRange(CalculateRange_Type SpecificCalculateRange)
    {
        firstPos = 4;
        secondPos = 4;

        isEquality = true;

        // Checking if its a special equality range.
        if (isEquality)
            flags = SC_ITERATOR_RANGE_VALID_LSKEY | SC_ITERATOR_RANGE_INCLUDE_LSKEY | SC_ITERATOR_RANGE_VALID_GRKEY | SC_ITERATOR_RANGE_INCLUDE_GRKEY;
        else
            flags = 0;

        // Pointing to range calculation function.
        CalculateRange = SpecificCalculateRange;
    }

    VOID AppendData(INT64 value, BOOL isNull, BOOL toFirst)
    {
        // Checking if we are adding to the first range point.
        if (toFirst)
        {
            // Checking if its a null(undefined) value.
            if (isNull)
            {
                first[firstPos] = 0;
                firstPos++;
                return;
            }

            // Defined value.
            first[firstPos] = 1;
            *(INT64 *) (first + firstPos + 1) = value;
            firstPos += 9;
        }
        else
        {
            // Checking if its a null(undefined) value.
            if (isNull)
            {
                second[secondPos] = 0;
                secondPos++;
                return;
            }

            // Defined value.
            second[secondPos] = 1;
            *(INT64 *) (second + secondPos + 1) = value;
            secondPos += 9;
        }
    }

    VOID AppendData(UINT64 value, BOOL isNull, BOOL toFirst)
    {
        // Checking if we are adding to the first range point.
        if (toFirst)
        {
            // Checking if its a null(undefined) value.
            if (isNull)
            {
                first[firstPos] = 0;
                firstPos++;
                return;
            }

            // Defined value.
            first[firstPos] = 1;
            *(UINT64 *) (first + firstPos + 1) = value;
            firstPos += 9;
        }
        else
        {
            // Checking if its a null(undefined) value.
            if (isNull)
            {
                second[secondPos] = 0;
                secondPos++;
                return;
            }

            // Defined value.
            second[secondPos] = 1;
            *(UINT64 *) (second + secondPos + 1) = value;
            secondPos += 9;
        }
    }

    UINT8 *GetFirst()
    {
        return first;
    }

    UINT8 *GetSecond()
    {
        // Checking if its an equality range.
        if (isEquality)
            return first;

        return second;
    }

    VOID FinalizeRange()
    {
        // Calling range calculation function first.
        CalculateRange(this);

        // Setting range point data lengths (to total length of the key).
        (*(UINT32 *)first) = firstPos;
        (*(UINT32 *)second) = secondPos;
    }

    VOID Reset()
    {
        firstPos = 4;
        secondPos = 4;

        flags = SC_ITERATOR_RANGE_VALID_LSKEY | SC_ITERATOR_RANGE_INCLUDE_LSKEY | SC_ITERATOR_RANGE_VALID_GRKEY | SC_ITERATOR_RANGE_INCLUDE_GRKEY;
        isEquality = true;
    }
};

struct Scan
{
    // Latest scan results.
    UINT64 oid;
    UINT64 eti;
    UINT16 ci;

    // Properties related to scan.
    const UINT64 indexHandle;
    UINT64 enumeratorCursor;
    UINT64 enumeratorVerify;

    // Specified scan range.
    ScanRange range;

    // Indicates enumerator created.
    BOOL enumCreated;

    // Constructor.
    Scan::Scan(UINT64 _indexHandle, CalculateRange_Type CalculateRange)
        : indexHandle(_indexHandle), range(CalculateRange)
    {
        oid = 0;
        eti = 0;
        ci = 0;

        enumeratorCursor = 0;
        enumeratorVerify = 0;

        enumCreated = false;
    }

    // Scan the range and retrieve fitting objects.
    INT32 MoveNext()
    {
        if (!enumCreated)
            CreateEnumerator();

        UINT64 dummy;

        // Iterating with created cursor.
        if (SCIteratorNext(enumeratorCursor, enumeratorVerify, &oid, &eti, &ci, &dummy) != 0)
            return -1;

        // Last object or object not found.
        if (oid == MDBIT_OBJECTID)
            return 1;

        return 0;
    }

    // Creates internal enumerator needed for the scan.
    INT32 CreateEnumerator()
    {
        // Finalizing search range.
        range.FinalizeRange();

        // Calling the enumerator.
        INT32 errCode = SCIteratorCreate(indexHandle, range.flags, range.GetFirst(), range.GetSecond(), &enumeratorCursor, &enumeratorVerify);

        // Setting creation flag on success.
        if (errCode == 0)
            enumCreated = true;

        return errCode;
    }

    // Cleans up all resources related to the scan.
    INT32 Reset()
    {
        INT32 errCode = SCIteratorFree(enumeratorCursor, enumeratorVerify);

        oid = 0;
        eti = 0;
        ci = 0;

        enumeratorCursor = 0;
        enumeratorVerify = 0;

        enumCreated = false;

        // Reseting scan range.
        range.Reset();

        return errCode;
    }

    // Reads integer property.
    INT32 ReadPropertyInt64(INT32 propIndex, INT64 *value, BOOL *isValueNull)
    {
        UINT16 flags = Mdb_ObjectReadInt64(oid, eti, propIndex, value);
        if ((flags & Mdb_DataValueFlag_Exceptional) == 0)
        {
            if ((flags & Mdb_DataValueFlag_Null) != 0)
                *isValueNull = true;

            return 0;
        }

        // Error occurred.
        return -1;
    }
};



// ENDING STATIC EXTERNAL CODE SECTION.

