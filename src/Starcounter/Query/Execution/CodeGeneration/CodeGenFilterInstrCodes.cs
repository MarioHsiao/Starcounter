
using System;

namespace Starcounter.Query.Execution
{
// Instruction codes for CodeGen filter 'language'.
internal static class CodeGenFilterInstrCodes
{
    // Instruction shifts for easier types handling.
    public const UInt32 UINT_INCR = 0;
    public const UInt32 SINT_INCR = 1;
    public const UInt32 FLT4_INCR = 2;
    public const UInt32 FLT8_INCR = 3;
    public const UInt32 BIN_INCR = 4;
    public const UInt32 STR_INCR = 5;
    public const UInt32 DEC_INCR = 6;
    public const UInt32 REF_INCR = 7;

    public const UInt32 LDV_BASE = 0; // Load variable value.
    // Instruction shift: 0
    public const UInt32 LDV_UINT = LDV_BASE + 0;
    public const UInt32 LDV_SINT = LDV_BASE + 1;
    public const UInt32 LDV_FLT4 = LDV_BASE + 2;
    public const UInt32 LDV_FLT8 = LDV_BASE + 3;
    public const UInt32 LDV_BIN = LDV_BASE + 4;
    public const UInt32 LDV_STR = LDV_BASE + 5;
    public const UInt32 LDV_DEC = LDV_BASE + 6;
    public const UInt32 LDV_REF = LDV_BASE + 7;


    public const UInt32 LDA_BASE = (LDV_BASE + 8); // Load attribute value. Requires index.
    // Instruction shift: 8
    public const UInt32 LDA_UINT = LDA_BASE + 0;
    public const UInt32 LDA_SINT = LDA_BASE + 1;
    public const UInt32 LDA_FLT4 = LDA_BASE + 2;
    public const UInt32 LDA_FLT8 = LDA_BASE + 3;
    public const UInt32 LDA_BIN = LDA_BASE + 4;
    public const UInt32 LDA_STR = LDA_BASE + 5;
    public const UInt32 LDA_DEC = LDA_BASE + 6;
    public const UInt32 LDA_REF = LDA_BASE + 7;


    public const UInt32 EQ_BASE = (LDA_BASE + 8); // Equal to compare.
    // Instruction shift: 16
    public const UInt32 EQ_UINT = EQ_BASE + 0;
    public const UInt32 EQ_SINT = EQ_BASE + 1;
    public const UInt32 EQ_FLT4 = EQ_BASE + 2;
    public const UInt32 EQ_FLT8 = EQ_BASE + 3;
    public const UInt32 EQ_BIN = EQ_BASE + 4;
    public const UInt32 EQ_STR = EQ_BASE + 5;
    public const UInt32 EQ_DEC = EQ_BASE + 6;
    public const UInt32 EQ_REF = EQ_BASE + 7;


    public const UInt32 NEQ_BASE = (EQ_BASE + 8); // Not equal to compare.
    // Instruction shift: 24
    public const UInt32 NEQ_UINT = NEQ_BASE + 0;
    public const UInt32 NEQ_SINT = NEQ_BASE + 1;
    public const UInt32 NEQ_FLT4 = NEQ_BASE + 2;
    public const UInt32 NEQ_FLT8 = NEQ_BASE + 3;
    public const UInt32 NEQ_BIN = NEQ_BASE + 4;
    public const UInt32 NEQ_STR = NEQ_BASE + 5;
    public const UInt32 NEQ_DEC = NEQ_BASE + 6;
    public const UInt32 NEQ_REF = NEQ_BASE + 7;


    public const UInt32 LS_BASE = (NEQ_BASE + 8); // Less then compare.
    // Instruction shift: 32
    public const UInt32 LS_UINT = LS_BASE + 0;
    public const UInt32 LS_SINT = LS_BASE + 1;
    public const UInt32 LS_FLT4 = LS_BASE + 2;
    public const UInt32 LS_FLT8 = LS_BASE + 3;
    public const UInt32 LS_BIN = LS_BASE + 4;
    public const UInt32 LS_STR = LS_BASE + 5;
    public const UInt32 LS_DEC = LS_BASE + 6;


    public const UInt32 LSE_BASE = (LS_BASE + 7); // Less then or equal to compare.
    /// Instruction shift: 39
    public const UInt32 LSE_UINT = LSE_BASE + 0;
    public const UInt32 LSE_SINT = LSE_BASE + 1;
    public const UInt32 LSE_FLT4 = LSE_BASE + 2;
    public const UInt32 LSE_FLT8 = LSE_BASE + 3;
    public const UInt32 LSE_BIN = LSE_BASE + 4;
    public const UInt32 LSE_STR = LSE_BASE + 5;
    public const UInt32 LSE_DEC = LSE_BASE + 6;

    public const UInt32 GR_BASE = (LSE_BASE + 7); // Greater then compare.
    // Instruction shift: 46
    public const UInt32 GR_UINT = GR_BASE + 0;
    public const UInt32 GR_SINT = GR_BASE + 1;
    public const UInt32 GR_FLT4 = GR_BASE + 2;
    public const UInt32 GR_FLT8 = GR_BASE + 3;
    public const UInt32 GR_BIN = GR_BASE + 4;
    public const UInt32 GR_STR = GR_BASE + 5;
    public const UInt32 GR_DEC = GR_BASE + 6;


    public const UInt32 GRE_BASE = (GR_BASE + 7); // Greater then or equal to compare.
    // Instruction shift: 53
    public const UInt32 GRE_UINT = GRE_BASE + 0;
    public const UInt32 GRE_SINT = GRE_BASE + 1;
    public const UInt32 GRE_FLT4 = GRE_BASE + 2;
    public const UInt32 GRE_FLT8 = GRE_BASE + 3;
    public const UInt32 GRE_BIN = GRE_BASE + 4;
    public const UInt32 GRE_STR = GRE_BASE + 5;
    public const UInt32 GRE_DEC = GRE_BASE + 6;

    public const UInt32 ISN = (GRE_BASE + 7);  // Is null.
    public const UInt32 INN = (ISN + 1);       // Is not null.
    public const UInt32 AND = (INN + 1);       // And.
    public const UInt32 OR  = (AND + 1);       // Or.

    public const UInt32 LDV_CREF = (OR + 1);
    public const UInt32 ISA = (LDV_CREF + 1);

    public const UInt32 CTD_BASE = (ISA + 1);  // Convert to decimal.

    public const UInt32 CTD_UINT = CTD_BASE + 0;
    public const UInt32 CTD_SINT = CTD_BASE + 1;
    public const UInt32 CTD_FLT4 = CTD_BASE + 2;
    public const UInt32 CTD_FLT8 = CTD_BASE + 3;

    public const UInt32 LDA_SREF = CTD_FLT8 + 1; // Reference to the whole object.

    public const UInt32 FILTER_INSTR_SET_SIZE = 70; // 70 instructions total.

    // Maximum number of instructions allowed for a single filter object.
    public const UInt32 FILTER_MAX_INSTR_COUNT = 220;

    // Maximum number of variables allowed for a single filter object.
    public const UInt32 FILTER_MAX_VAR_COUNT = 31;

    // Maximum stack size in bytes is 1024. This value is calculated from the
    // maximum data needed when verifying the filter.
    public const UInt32 FILTER_MAX_STACK_SIZE = 40;
}
}
