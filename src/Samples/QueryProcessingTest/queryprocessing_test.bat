:: Checking if test should be run.
IF "%SC_RUN_QUERYPROCESSING_TEST%"=="False" GOTO :EOF

:: Some predefined constants.
SET DB_DIR=.db
SET DB_OUT_DIR=.db.output
SET DB_NAME=QUERYPROCESSINGTEST
SET TEST_NAME=QueryProcessingTest

:: Killing all SC processes.
staradmin kill all

:: Checking for existing dirs.
IF EXIST .db (
    RMDIR .db /S /Q
    RMDIR .db.output /S /Q
)

IF EXIST s\QueryProcessingTest\dumpQueryProcessingDB.sql DEL s\QueryProcessingTest\dumpQueryProcessingDB.sql

:: Checking if directories exist.
IF NOT EXIST %DB_DIR% ( MKDIR %DB_DIR% )
IF NOT EXIST %DB_OUT_DIR% ( MKDIR %DB_OUT_DIR% )

:: Creating image files.
sccreatedb.exe -ip %DB_DIR% -uuid "def000db-dfdb-dfdb-dfdb-def0db0df0db" %DB_NAME%

:: Weaving the test.
CALL scweaver.exe --FLAG:disableeditionlibraries "s\%TEST_NAME%\%TEST_NAME%.exe"
IF %ERRORLEVEL% NEQ 0 (

    ECHO Error: The query processing regression test failed!
    EXIT /b 1
)

:: Starting IPC monitor first.
::START CMD /C "scipcmonitor.exe PERSONAL %DB_OUT_DIR%"

:: Path to signed assembly.
SET TEST_WEAVED_ASSEMBLY=s\%TEST_NAME%\.starcounter\%TEST_NAME%.exe

:: Re-signing the assembly.
"c:\Program Files (x86)\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools\sn.exe" -R "%TEST_WEAVED_ASSEMBLY%" "..\..\src\Starcounter.snk"
IF %ERRORLEVEL% NEQ 0 (

    ECHO Error: Re-signing the assembly failed!
    EXIT /b 1
)

:: Starting database memory management process.
powershell -ExecutionPolicy Unrestricted -File %~dp0\..\..\src\tests\start_scdata.ps1 %~dp0 %DB_NAME% %DB_DIR% %DB_OUT_DIR% def000db-dfdb-dfdb-dfdb-def0db0df0db

:: Starting Prolog process.
START CMD /C "32bitComponents\scsqlparser.exe 8066"

:: Starting database with some delay.
sccode.exe "def000db-dfdb-dfdb-dfdb-def0db0df0db" %DB_NAME% --DatabaseDir=%DB_DIR% --OutputDir=%DB_OUT_DIR% --TempDir=%DB_OUT_DIR% --AutoStartExePath="%TEST_WEAVED_ASSEMBLY%" --FLAG:NoNetworkGateway

IF %ERRORLEVEL% NEQ 0 (

    ECHO Error: The query processing regression test failed!
    EXIT /b 1
)

sccode.exe "def000db-dfdb-dfdb-dfdb-def0db0df0db" %DB_NAME% --DatabaseDir=%DB_DIR% --OutputDir=%DB_OUT_DIR% --TempDir=%DB_OUT_DIR% --AutoStartExePath="%TEST_WEAVED_ASSEMBLY%" --FLAG:NoNetworkGateway

IF %ERRORLEVEL% NEQ 0 (

    ECHO Error: The query processing regression test failed!
    EXIT /b 1
	
) else (

    ECHO The query processing regression test succeeded.
    EXIT /b 0
)
