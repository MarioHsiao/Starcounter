
:: Some predefined constants.
SET DB_DIR=.db
SET DB_OUT_DIR=.db.output
SET DB_NAME=QUERYPROCESSINGTEST
SET TEST_NAME=QueryProcessingTest

sccode.exe %DB_NAME% --OutputDir=%DB_OUT_DIR% --TempDir=%DB_OUT_DIR% --AutoStartExePath="%TEST_WEAVED_ASSEMBLY%" --FLAG:NoNetworkGateway
IF ERRORLEVEL 1 (
ECHO Error: The query processing regression test failed!
EXIT /b 1
) else (
ECHO The query processing regression test succeeded.
EXIT /b 0
)
