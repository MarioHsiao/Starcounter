@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_NOTALLLOADED%"=="False" GOTO :EOF

ECHO Running NotAllLoaded regression test.

REM Some predefined constants.
SET DB_NAME=NotAllLoadedDb

if "%SC_RUNNING_ON_BUILD_SERVER%"=="True" GOTO skipdbdrop
ECHO Delete database after server is started
staradmin start server
staradmin --database=%DB_NAME% delete --force db

:skipdbdrop

ECHO Run Step 1 to create initial schema and index
COPY /y NotAllLoadedV1.cs NotAllLoaded.cs
star --database=%DB_NAME% NotAllLoaded.cs
IF %ERRORLEVEL% NEQ 0 GOTO err

ECHO Run Step 2 to update initial schema without indexed column
COPY /y NotAllLoadedV2.cs NotAllLoaded.cs
star --database=%DB_NAME% NotAllLoaded.cs
IF %ERRORLEVEL% NEQ 0 GOTO err

REM Clean update
DEL NotAllLoaded.cs
staradmin --database=%DB_NAME% stop db

ECHO NotAllLoaded regression test succeeded.
EXIT /b 0


:err
DEL NotAllLoaded.cs
ECHO Error: NotAllLoaded failed!
EXIT /b 1
