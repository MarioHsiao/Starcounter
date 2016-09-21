
:: Some predefined constants.
SET DB_DIR=.db
SET DB_OUT_DIR=.db.output
SET DB_NAME=QUERYPROCESSINGTEST
SET TEST_NAME=QueryProcessingTest

sccode.exe "def000db-dfdb-dfdb-dfdb-def0db0df0db" %DB_NAME% --DatabaseDir=%DB_DIR% --OutputDir=%DB_OUT_DIR% --TempDir=%DB_OUT_DIR% --AutoStartExePath="%TEST_WEAVED_ASSEMBLY%" --FLAG:NoNetworkGateway
IF %ERRORLEVEL% NEQ 0 (
ECHO Error: The query processing regression test failed!
EXIT /b 1
) else (
ECHO The query processing regression test succeeded.
EXIT /b 0
)
