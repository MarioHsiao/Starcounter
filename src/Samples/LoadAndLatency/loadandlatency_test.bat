:: Some predefined constants.
SET DB_NAME=LOADANDLATENCY
SET TEST_NAME=LoadAndLatency
SET TEST_ARGS=SpecificTestType=0 MinNightlyWorkers=9 %*

staradmin start server
staradmin --database=%DB_NAME% delete --force db

:: Starting database with some delay.
:: sccode.exe %DB_NAME% --DatabaseDir=%DB_DIR% --OutputDir=%DB_OUT_DIR% --TempDir=%DB_OUT_DIR% --AutoStartExePath=s\%TEST_NAME%\.starcounter\%TEST_NAME%.exe --FLAG:NoNetworkGateway %TEST_ARGS%
star --database=%DB_NAME% s\%TEST_NAME%\%TEST_NAME%.exe %TEST_ARGS%

IF ERRORLEVEL 1 (
    ECHO Error: LoadAndLatency test failed!
    EXIT /b 1
) else (
    staradmin --database=%DB_NAME% stop db
	staradmin --database=%DB_NAME% delete --force db	
    ECHO LoadAndLatency test succeeded.
    EXIT /b 0
)