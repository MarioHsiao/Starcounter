OUTPUT_NAME = scerrres

# Path to error code XML file.
EC_XML = ..\errorcodes.xml

# Path to error code compiler EXE.
ECC = ..\ErrorCodeCompiler\scerrcc.exe

# Path to formatter source code files
Format_H = format.h
Format_C = Format.cpp
Format_Def = Format.def

# Hierarchical dependency building rules:

"$(OUTDIR)\$(OUTPUT_NAME).mc": "$(EC_XML)"
    if not exist "$(OUTDIR)" mkdir "$(OUTDIR)"
    "$(ECC)" "$(EC_XML)" -mc "$(OUTDIR)\$(OUTPUT_NAME).mc"

# Header file is generated in the current project directory.
"$(OUTPUT_NAME).h" "$(OUTDIR)\$(OUTPUT_NAME).rc": "$(OUTDIR)\$(OUTPUT_NAME).mc"
    attrib -R "$(OUTPUT_NAME).h"
    mc.exe -r "$(OUTDIR)" -v "$(OUTDIR)\$(OUTPUT_NAME).mc"

"$(OUTDIR)\$(OUTPUT_NAME).res": "$(OUTPUT_NAME).h" "$(OUTDIR)\$(OUTPUT_NAME).mc" "$(OUTDIR)\$(OUTPUT_NAME).rc"
    rc.exe /I "$(OUTDIR)" /R /V "$(OUTDIR)\$(OUTPUT_NAME).rc"

"$(OUTDIR)\$(OUTPUT_NAME).obj": "$(Format_H)" "$(Format_C)" "$(Format_Def)"
    cl.exe /c /D "FORMATMESSAGE_EXPORTS" "$(Format_C)" /Fo"$(OUTDIR)\$(OUTPUT_NAME).obj" /DEBUG /Zi /Fd"$(OUTDIR)\$(OUTPUT_NAME).pdb"

"$(OUTDIR)\$(OUTPUT_NAME).dll": "$(OUTPUT_NAME).h" "$(OUTDIR)\$(OUTPUT_NAME).mc" "$(OUTDIR)\$(OUTPUT_NAME).rc" "$(OUTDIR)\$(OUTPUT_NAME).res" "$(OUTDIR)\$(OUTPUT_NAME).obj"
    link.exe /MACHINE:$(MACHINE) /DLL /DEBUG /PDB:"$(OUTDIR)\$(OUTPUT_NAME).pdb" /DEF:"$(Format_Def)" /OUT:"$(OUTDIR)\$(OUTPUT_NAME).dll" "$(OUTDIR)\$(OUTPUT_NAME).res" "$(OUTDIR)\$(OUTPUT_NAME).obj"

all: "$(OUTDIR)\$(OUTPUT_NAME).dll"

clean:
    rmdir /Q /S "$(OUTDIR)"