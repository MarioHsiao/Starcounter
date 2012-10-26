:: @ECHO OFF

::IF "%1"=="" (
::ECHO Error: Please specify the path to Starcounter bin directory.
::GOTO ERROR
::)

:: clean up the database and processes
CMD /C kill_all.bat 2> NUL
IF EXIST .db (
RMDIR .db /S /Q
RMDIR .db.output /S /Q
)
IF EXIST SQLTest RMDIR SQLTest /S /Q
:: create the database
MKDIR .db
MKDIR .db.output
CMD /C scdbc.exe -ip .db -lp .db SqlTest
:: start servers
START ScConnMonitor PERSONAL .db.output
START scpmm SQLTEST SqlTest .db.output
:: start the program
CMD /C Weaver.exe s\SQLTest\SQLTest.exe --FLAG:tocache
boot SQLTEST --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --CompilerPath=MinGW\bin\x86_64-w64-mingw32-gcc.exe --AutoStartExePath="s\SQLTest\.starcounter\SQLTest.exe" --FLAG:UseConsole  --FLAG:NoNetworkGateway
:: clean up
CMD /C kill_all.bat 2> NUL
RMDIR .db /S /Q
RMDIR .db.output /S /Q
RMDIR SQLTest /S /Q
rem return exit code
IF NOT EXIST s\Starcounter\failedTest EXIT 0

DEL %1\s\Starcounter\failedTest
ECHO Error: The regression test is failed!
:ERROR
EXIT 1
