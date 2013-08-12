#include <windows.h>
#include "cpuid.hpp"

// Checks for processor CPUID options.
extern "C" void __stdcall sc_check_cpu_features(bool* popcnt_instr)
{
    CPUID cpu_id;

    // Get Processor Info and Feature Bits.
    cpu_id.load(1);

    // http://en.wikipedia.org/wiki/CPUID
    // http://msdn.microsoft.com/en-us/library/hskdteyh(v=vs.100).aspx
    // POPCNT is in 23 bit of ECX.

    //*popcnt_instr = ((cpu_id.ECX() & 0x00800000) != 0);

    // Calling popcnt instruction directly.
    *popcnt_instr = (32 == _mm_popcnt_u64(0xf0f0f0f0f0f0f0f0ULL));
}
