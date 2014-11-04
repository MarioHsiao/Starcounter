@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_SELECTONNULL2362%"=="False" GOTO :EOF

ECHO Running SelectOnNull2362 regression test.

REM Some predefined constants.
SET DB_NAME=SelectOnNull2362Db

REM Delete database after server is started
REM staradmin --database=%DB_NAME% delete

star --database=%DB_NAME% SelectOnNull2362.cs
IF %ERRORLEVEL% NEQ 0 GOTO err


ECHO SelectOnNull2362 regression test succeeded.
EXIT /b 0


:err
ECHO Error: SelectOnNull2362 failed!
EXIT /b 1
