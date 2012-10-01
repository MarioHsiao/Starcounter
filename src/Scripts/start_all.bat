@ECHO OFF

:: First parameter is database/test name in capital letters, e.g.: LOADANDLATENCY, SQLTEST1, etc.
IF "%1"=="" (
ECHO Please specify database/test name as a first argument in CAPITALIZED lettes, e.g.: LOADANDLATENCY, SQLTEST1, etc.
GOTO:EOF
)

:: Creating database if needed.
START "scdbc" scdbc -ip .db -lp .db %1

:: Starting database memory management process.
START "scpmm" scpmm %1 %1 .db.output

:: Starting connection monitor on PERSONAL server.
START "ScConnMonitor" ScConnMonitor PERSONAL .db.output

:: Starting the specific database.
START "boot" boot %1 --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --CompilerPath=MinGW\bin\x86_64-w64-mingw32-gcc.exe