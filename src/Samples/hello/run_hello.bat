
:: Some predefined constants.
SET DB_DIR=.db
SET DB_OUT_DIR=.db.output
SET DB_NAME=HELLO
SET TEST_NAME=hello

:: Killing all SC processes.
staradmin kill all

:: Checking for existing dirs.
IF EXIST .db (
    RMDIR .db /S /Q
    RMDIR .db.output /S /Q
)

:: Checking if directories exist.
IF NOT EXIST %DB_DIR% ( MKDIR %DB_DIR% )
IF NOT EXIST %DB_OUT_DIR% ( MKDIR %DB_OUT_DIR% )

:: Creating image files.
sccreatedb.exe -ip %DB_DIR% %DB_NAME%

:: Weaving the test.
CALL scweaver.exe "s\%TEST_NAME%\%TEST_NAME%.exe"
IF %ERRORLEVEL% NEQ 0 (

    ECHO Error: The query processing regression test failed!
    EXIT /b 1
)

:: Starting IPC monitor first.
START CMD /C "scipcmonitor.exe PERSONAL %DB_OUT_DIR%"

:: Path to signed assembly.
SET TEST_WEAVED_ASSEMBLY=s\%TEST_NAME%\.starcounter\%TEST_NAME%.exe

:: Re-signing the assembly.
"c:\Program Files (x86)\Microsoft SDKs\Windows\v8.0A\bin\NETFX 4.0 Tools\sn.exe" -R "%TEST_WEAVED_ASSEMBLY%" "..\..\src\Starcounter.snk"

:: Starting database memory management process.
START CMD /C "scdata.exe -instid 1 "{ \"eventloghost\": \"%DB_NAME%\", \"eventlogdir\": \"%DB_OUT_DIR%\", \"databasename\": \"%DB_NAME%\", \"databasedir\": \"%DB_DIR%\" }""

:: Starting Prolog process.
START CMD /C "32bitComponents\scsqlparser.exe 8066"

:: Sleeping some time using ping.
ping -n 3 127.0.0.1 > nul

:: Starting database with some delay.
sccode.exe 1 %DB_NAME% --DatabaseDir=%DB_DIR% --OutputDir=%DB_OUT_DIR% --TempDir=%DB_OUT_DIR% --AutoStartExePath="%TEST_WEAVED_ASSEMBLY%" --FLAG:NoNetworkGateway

IF %ERRORLEVEL% NEQ 0 (

    ECHO Error: Hello failed!
    EXIT /b 1
)
