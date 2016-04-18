:: Checking if test should be run.
IF "%SC_RUN_SQL_REGRESSION_TEST%"=="False" GOTO :EOF

:: @ECHO OFF

:: Killing all SC processes.
staradmin kill all

IF EXIST .db (
    RMDIR .db /S /Q
    RMDIR .db.output /S /Q
)

IF EXIST SQLTest RMDIR SQLTest /S /Q

:: create the database
MKDIR .db
MKDIR .db.output
CMD /C sccreatedb.exe -ip .db SqlTest

:: weave the application
CMD /C scweaver.exe --FLAG:disableeditionlibraries s\SQLTest\SQLTest.exe

IF %ERRORLEVEL% NEQ 0 (
    ECHO Error: SQL regression test is failed!
    EXIT /b 1
)

:: start servers
::START scipcmonitor.exe PERSONAL .db.output
START scdata.exe 1 SQLTEST .db.output SqlTest .db
START scdata.exe -instid 1 "{ \"hostname\": \"SQLTEST\", \"eventlogdir\": \".db.output\", \"databasename\": \"SqlTest\", \"logdir\": \".db\" }"
START 32bitComponents\scsqlparser.exe 8066

:: Sleeping some time using ping.
ping -n 5 127.0.0.1 > nul

:: start the program
CALL sccode.exe 1 SQLTEST --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --AutoStartExePath="s\SQLTest\.starcounter\SQLTest.exe" --FLAG:NoNetworkGateway

IF %ERRORLEVEL% NEQ 0 (
    ECHO Error: SQL regression test is failed!
    EXIT /b 1
) ELSE (
    ECHO SQL regression test succeeded.
    EXIT /b 0
)
