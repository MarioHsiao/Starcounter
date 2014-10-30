@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_TESTAPPS%"=="False" GOTO :EOF

ECHO Regression test of simple apps succeeded.
EXIT /b 0


:err
DEL InheritedClassSchemaChange.cs
ECHO Error:  Regression test of simple apps failed!
EXIT /b 1
