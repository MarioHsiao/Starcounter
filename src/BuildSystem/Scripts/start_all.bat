@ECHO OFF

:: First parameter is database/test name in capital letters, e.g.: LOADANDLATENCY, SQLTEST1, etc.
IF "%1"=="" (
ECHO Please specify database/test name as a first argument in CAPITALIZED lettes, e.g.: LOADANDLATENCY, SQLTEST1, etc.
GOTO:EOF
)

:: Killing everything!
CMD /C kill_all.bat 2> NUL

:: Checking if everything is pre-created.
IF EXIST .db (
RMDIR .db /S /Q
RMDIR .db.output /S /Q
)

:: Creating directories.
MKDIR .db
MKDIR .db.output

:: Creating database.
sccreatedb.exe -ip .db -lp .db %1

:: Starting database memory management process.
START "scdata" scdata.exe %1 %1 .db.output

:: Starting connection monitor on PERSONAL server.
START "scipcmonitor" scipcmonitor.exe PERSONAL .db.output

:: Starting the specific database.
::START "sccode" sccode.exe %1 --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --CompilerPath=MinGW\bin\x86_64-w64-mingw32-gcc.exe --AutoStartExePath=C:\sc\Level1\src\Samples\MySampleApp\bin\debug\mysampleapp.exe

:: Auto-start example.
::sccode.exe NETWORKIOTEST --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --CompilerPath=MinGW\bin\x86_64-w64-mingw32-gcc.exe --AutoStartExePath=c:\github\Orange\bin\Debug\NetworkIoTest\NetworkIoTest.exe

:: Starting Network Gateway.
START "scnetworkgateway" scnetworkgateway.exe PERSONAL scnetworkgateway.xml .db.output
