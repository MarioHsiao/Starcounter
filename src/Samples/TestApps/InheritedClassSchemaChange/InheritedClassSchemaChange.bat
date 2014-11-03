@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_INHERITEDCLASSSCHEMACHANGE%"=="False" GOTO :EOF

ECHO Running InheritedClassSchemaChange regression test.

REM Some predefined constants.
SET DB_NAME=InheritedClassSchemaChangeDb

REM Delete database after server is started
REM staradmin --database=%DB_NAME% delete

ECHO Run Step 1 to create initial schema
COPY /y InheritedClassSchemaChange.Step1.cs InheritedClassSchemaChange.cs
star --database=%DB_NAME% InheritedClassSchemaChange.cs
IF %ERRORLEVEL% NEQ 0 GOTO err

ECHO Run Step 2 to update initial schema with more columns
COPY /y InheritedClassSchemaChange.Step2.cs InheritedClassSchemaChange.cs
star --database=%DB_NAME% InheritedClassSchemaChange.cs
IF %ERRORLEVEL% NEQ 0 GOTO err

ECHO Run Step 1 again to update scheme with less columns
COPY /y InheritedClassSchemaChange.Step1.cs InheritedClassSchemaChange.cs
star --database=%DB_NAME% InheritedClassSchemaChange.cs
IF %ERRORLEVEL% NEQ 0 GOTO err

REM Clean update
DEL InheritedClassSchemaChange.cs

ECHO InheritedClassSchemaChange regression test succeeded.
EXIT /b 0


:err
DEL InheritedClassSchemaChange.cs
ECHO Error: InheritedClassSchemaChange failed!
EXIT /b 1
