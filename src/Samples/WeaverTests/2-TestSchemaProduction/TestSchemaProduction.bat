@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_TESTSCHEMAPRODUCTION%"=="False" GOTO :EOF

REM Some predefined constants.
SET TEST_NAME=TestSchemaProduction
SET SRC_FILE=%TEST_NAME%.cs
SET EXE_FILE=%TEST_NAME%.exe

ECHO Running %TEST_NAME% regression test.

star --sc-compile %SRC_FILE%
IF ERRORLEVEL 1 GOTO err_compile
scweaver %EXE_FILE%
IF ERRORLEVEL 1 GOTO err_weave
scweaver Test %EXE_FILE%
IF ERRORLEVEL 1 GOTO err_run

ECHO %TEST_NAME% regression test succeeded.
EXIT /b 0

:err_compile
GOTO err

:err_weave
GOTO err

:err_run
GOTO err

:err
ECHO Error: %TEST_NAME% failed!
EXIT /b 1
