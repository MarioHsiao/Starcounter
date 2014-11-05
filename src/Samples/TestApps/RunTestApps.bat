@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_TESTAPPS%"=="False" GOTO :EOF

PUSHD 3LevelSchemaChange
CALL 3LevelSchemaChange.bat
POPD
IF %ERRORLEVEL% NEQ 0 GOTO err

PUSHD TestClassSchemaChange
CALL TestClassSchemaChange.bat
POPD
IF %ERRORLEVEL% NEQ 0 GOTO err

REM PUSHD SelectOnNull2362
REM CALL SelectOnNull2362.bat
REM POPD
REM IF %ERRORLEVEL% NEQ 0 GOTO err

ECHO Regression test of simple apps succeeded.
EXIT /b 0


:err
ECHO Error:  Regression test of simple apps failed!
EXIT /b 1
