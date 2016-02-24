@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_DropTableBug3387%"=="False" GOTO :EOF

ECHO Running DropTableBug3387 regression test.

REM Some predefined constants.
SET DB_NAME=DropTableBug3387Db

if "%SC_RUNNING_ON_BUILD_SERVER%"=="True" GOTO skipdbdrop
ECHO Delete database after server is started
staradmin start server
staradmin --database=%DB_NAME% delete --force db

:skipdbdrop

ECHO Run Step 1 to create initial schema
COPY /y DropTableBug3387_step1.cs DropTableBug3387.cs
star --database=%DB_NAME% DropTableBug3387.cs
IF %ERRORLEVEL% NEQ 0 GOTO err

ECHO Run Step 2 to drop the table
COPY /y DropTableBug3387_step2.cs DropTableBug3387.cs
star --database=%DB_NAME% DropTableBug3387.cs
IF %ERRORLEVEL% NEQ 0 GOTO err

ECHO Run Step 1 to reproduce the bug or test the fix
COPY /y DropTableBug3387_step1.cs DropTableBug3387.cs
star --database=%DB_NAME% DropTableBug3387.cs
IF %ERRORLEVEL% NEQ 0 GOTO err

REM Clean update
DEL DropTableBug3387.cs
staradmin --database=%DB_NAME% stop db

ECHO DropTableBug3387 regression test succeeded.
EXIT /b 0


:err
DEL DropTableBug3387.cs
ECHO Error: DropTableBug3387 failed!
EXIT /b 1
