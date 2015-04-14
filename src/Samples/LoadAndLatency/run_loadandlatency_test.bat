:: Checking if test should be run.
IF "%SC_RUN_LOADANDLATENCY_TEST%"=="False" GOTO :EOF

:: Some predefined constants.
SET DB_DIR=.db
SET DB_OUT_DIR=.db.output
SET DB_NAME=LOADANDLATENCY
SET TEST_NAME=LoadAndLatency
SET TEST_ARGS=--UserArguments="SpecificTestType=0 %*"

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

:: Starting IPC monitor first.
START CMD /C "scipcmonitor.exe PERSONAL %DB_OUT_DIR%"

:: Weaving the test.
scweaver.exe "s\%TEST_NAME%\%TEST_NAME%.exe"

:: Starting database memory management process.
START CMD /C "scdata.exe 0 %DB_NAME% %DB_OUT_DIR%" %DB_NAME% %DB_DIR% %DB_DIR%

:: Starting log writer process.
START CMD /C "scdblog.exe %DB_NAME% %DB_NAME% %DB_OUT_DIR%"

:: Starting Prolog process.
START CMD /C "32bitComponents\scsqlparser.exe 8066"

:: Sleeping some time using ping.
ping -n 3 127.0.0.1 > nul

:: Starting database with some delay.
sccode.exe %DB_NAME% 0 --OutputDir=%DB_OUT_DIR% --TempDir=%DB_OUT_DIR% --AutoStartExePath=s\%TEST_NAME%\.starcounter\%TEST_NAME%.exe --FLAG:NoNetworkGateway %TEST_ARGS%

IF ERRORLEVEL 1 (
    ECHO Error: LoadAndLatency test failed!
    EXIT /b 1
) else (
    ECHO LoadAndLatency test succeeded.
    EXIT /b 0
)