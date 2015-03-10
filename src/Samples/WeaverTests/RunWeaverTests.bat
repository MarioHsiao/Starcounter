@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_WEAVERTESTS%"=="False" GOTO :EOF

PUSHD 1-TestTestProtocol
CALL TestTestProtocol.bat
POPD
IF ERRORLEVEL 1 GOTO err

ECHO Weaver regression tests succeeded.
EXIT /b 0

:err
ECHO Error:  Weaver regression tests failed!
EXIT /b 1