@echo off

:: Checking if documentation should be generated.
IF "%SC_GENERATE_CODE_DOCS%"=="False" GOTO :EOF

:: Checking if DocumentX exist.
set DocXPath=C:\Program Files (x86)\Innovasys\DocumentX2015\bin\DocumentXCommandLinex64.exe
IF NOT EXIST "%DocXPath%" set DocXPath=C:\Program Files (x86)\Innovasys\DocumentX2013\bin\DocumentXCommandLinex64.exe
IF NOT EXIST "%DocXPath%" GOTO :EOF

call "%DocXPath%" ".\public\starcounter.dxp" [/buildconfiguration="Public"]
call "%DocXPath%" ".\internal\starcounter.dxp" [/buildconfiguration="Internal"]
