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
CMD /C sccreatedb.exe -ip .db -uuid "def000db-dfdb-dfdb-dfdb-def0db0df0db" SqlTest

:: weave the application
CMD /C scweaver.exe --FLAG:disableeditionlibraries s\SQLTest\SQLTest.exe

IF %ERRORLEVEL% NEQ 0 (
    ECHO Error: SQL regression test is failed!
    EXIT /b 1
)

:: start servers
powershell -ExecutionPolicy Unrestricted -File %~dp0\..\..\src\tests\start_scdata.ps1 %~dp0 SQLTEST .db .db.output def000db-dfdb-dfdb-dfdb-def0db0df0db

START 32bitComponents\scsqlparser.exe 8066

:: start the program
CALL sccode.exe "def000db-dfdb-dfdb-dfdb-def0db0df0db" SQLTEST --DatabaseDir=.db --OutputDir=.db.output --TempDir=.db.output --AutoStartExePath="s\SQLTest\.starcounter\SQLTest.exe" --FLAG:NoNetworkGateway

IF %ERRORLEVEL% NEQ 0 (
    ECHO Error: SQL regression test is failed!
    EXIT /b 1
) ELSE (
    ECHO SQL regression test succeeded.
    EXIT /b 0
)
