@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_3LEVELSCHEMACHANGE%"=="False" GOTO :EOF

ECHO Running 3LevelSchemaChange regression test.

REM Some predefined constants.
SET DB_NAME=3LevelSchemaChangeDb
REM SET DB_NAME=TestAppsDb

REM Delete database after server is started
REM staradmin --database=%DB_NAME% delete

ECHO Run Step 1 to create initial schema
COPY /y 3LevelSchemaChange.Step1.cs 3LevelSchemaChange.cs
star --database=%DB_NAME% 3LevelSchemaChange.cs
IF %ERRORLEVEL% NEQ 0 GOTO err

ECHO Run Step 2 to update the initial schema with more columns
COPY /y 3LevelSchemaChange.Step2.cs 3LevelSchemaChange.cs
star --database=%DB_NAME% 3LevelSchemaChange.cs
IF %ERRORLEVEL% NEQ 0 GOTO err

ECHO Run Step 1 again to update the schema with less columns
COPY /y 3LevelSchemaChange.Step1.cs 3LevelSchemaChange.cs
star --database=%DB_NAME% 3LevelSchemaChange.cs
IF %ERRORLEVEL% NEQ 0 GOTO err

REM Clean update
DEL 3LevelSchemaChange.cs

ECHO 3LevelSchemaChange regression test succeeded.
EXIT /b 0


:err
DEL 3LevelSchemaChange.cs
ECHO Error: 3LevelSchemaChange failed!
EXIT /b 1