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

IF %ERRORLEVEL% NEQ 0 (
    ECHO Error: SQL regression test is failed!
    EXIT /b 1
)

:: start servers
START scipcmonitor.exe PERSONAL .db.output
START scdata.exe 1 SQLTEST .db.output SqlTest .db
START 32bitComponents\scsqlparser.exe 8066
