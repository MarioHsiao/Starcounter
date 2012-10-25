:: @ECHO OFF

IF "%1"=="" (
ECHO Error: Please specify the path to Starcounter bin directory.
GOTO ERROR
)

:: clean up the database and processes
CMD /C %1\kill_all.bat 2> NUL
IF EXIST .db (
RMDIR %1\.db /S /Q
RMDIR %1\.db.output /S /Q
)
IF EXIST %1\SQLTest RMDIR %1\SQLTest /S /Q
:: create the database
MKDIR %1\.db
MKDIR %1\.db.output
CMD /C %1\scdbc.exe -ip .db -lp .db SqlTest
:: start servers
START %1\ScConnMonitor PERSONAL .db.output
START %1\scpmm SQLTEST SqlTest .db.output
:: start the program
CMD /C %1\Weaver.exe %1\s\SQLTest\SQLTest.exe --FLAG:tocache
%1\boot SQLTEST --DatabaseDir=%1\.db --OutputDir=%1\.db.output --TempDir=%1\.db.output --CompilerPath=%1\MinGW\bin\x86_64-w64-mingw32-gcc.exe --AutoStartExePath="s\SQLTest\.starcounter\SQLTest.exe" --FLAG:UseConsole  --FLAG:NoNetworkGateway
:: clean up
CMD /C %1\kill_all.bat 2> NUL
RMDIR %1\.db /S /Q
RMDIR %1\.db.output /S /Q
RMDIR %1\SQLTest /S /Q
rem return exit code
IF NOT EXIST %1\s\Starcounter\failedTest EXIT 0

DEL %1\s\Starcounter\failedTest
ECHO Error: The regression test is failed!
:ERROR
EXIT 1
