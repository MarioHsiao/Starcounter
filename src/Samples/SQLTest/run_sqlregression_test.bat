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
CMD /C scweaver.exe s\SQLTest\SQLTest.exe

IF ERRORLEVEL 1 (
    ECHO Error: SQL regression test is failed!
    EXIT /b 1
)

:: start servers
START scipcmonitor.exe PERSONAL .db.output
START scdata.exe 0 SqlTest .db.output SQLTEST .db .db
START scdblog.exe SqlTest SqlTest .db.output
START 32bitComponents\scsqlparser.exe 8066

CALL sccode.exe SQLTEST --OutputDir=.db.output --TempDir=.db.output --AutoStartExePath="s\SQLTest\.starcounter\SQLTest.exe" --FLAG:NoNetworkGateway

IF ERRORLEVEL 1 (
    ECHO Error: SQL regression test is failed!
    EXIT /b 1
) ELSE (
    ECHO SQL regression test succeeded.
    EXIT /b 0
)
