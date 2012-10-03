@ECHO OFF

:: First parameter is database/test name in capital letters, e.g.: LOADANDLATENCY, SQLTEST1, etc.
IF "%1"=="" (
ECHO Please specify database/test name as a first argument in CAPITALIZED lettes, e.g.: LOADANDLATENCY, SQLTEST1, etc.
GOTO:EOF
)

:: Checking if everything is pre-created.
IF NOT EXIST .db (
:: Creating directories.
mkdir .db
mkdir .db.output

:: Creating database.
scdbc.exe -ip .db -lp .db %1
)

:: Starting database memory management process.
START "scpmm" scpmm.exe %1 %1 .db.output

:: Starting connection monitor on PERSONAL server.
START "ScConnMonitor" ScConnMonitor.exe PERSONAL .db.output

:: Starting the specific database.
START "boot" boot.exe %1 --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --CompilerPath=MinGW\bin\x86_64-w64-mingw32-gcc.exe

:: Auto-start example.
::boot.exe NETWORKIOTEST --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --CompilerPath=MinGW\bin\x86_64-w64-mingw32-gcc.exe --AutoStartExePath=c:\github\Orange\bin\Debug\NetworkIoTest\NetworkIoTest.exe
