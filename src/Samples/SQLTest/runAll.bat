:: @ECHO OFF

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
CALL  boot SQLTEST --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --CompilerPath=MinGW\bin\x86_64-w64-mingw32-gcc.exe --AutoStartExePath="s\SQLTest\.starcounter\SQLTest.exe" --FLAG:UseConsole  --FLAG:NoNetworkGateway
IF %ERRORLEVEL% NEQ 0 (
:: clean up and exit code on fail
CMD /C kill_all.bat 2> NUL
REM RMDIR .db /S /Q
REM RMDIR .db.output /S /Q
REM RMDIR SQLTest /S /Q
ECHO Error: The regression test is failed!
EXIT 1
) ELSE (
:: clean up and exit code on success
CMD /C kill_all.bat 2> NUL
REM RMDIR .db /S /Q
REM RMDIR .db.output /S /Q
REM RMDIR SQLTest /S /Q
ECHO Regression test succeeded.
EXIT 0
)
