@ECHO OFF
REM Checking if test should be run.
IF "%SC_RUN_TESTAPPS%"=="False" GOTO :EOF

:: For this test no extra database classes should be present, so 
:: renaming temporary LibrariesWithDatabaseClasses and EditionLibraries directory.
PUSHD "%StarcounterBin%"
IF EXIST LibrariesWithDatabaseClasses ( RENAME LibrariesWithDatabaseClasses DontUseLibrariesWithDatabaseClasses )
IF EXIST EditionLibraries ( RENAME EditionLibraries DontUseEditionLibraries )
POPD

PUSHD 3LevelSchemaChange
CALL 3LevelSchemaChange.bat
POPD
IF ERRORLEVEL 1 GOTO err

PUSHD TestClassSchemaChange
CALL TestClassSchemaChange.bat
POPD
IF ERRORLEVEL 1 GOTO err

PUSHD IndexedColumnDrop
CALL IndexedColumnDrop.bat
POPD
IF ERRORLEVEL 1 GOTO err

PUSHD DropTable
CALL DropTable.bat
POPD
IF ERRORLEVEL 1 GOTO err

REM The test is excluded, since the bug 2462 was not fixed
REM PUSHD SelectOnNull2362
REM CALL SelectOnNull2362.bat
REM POPD
REM IF ERRORLEVEL 1 GOTO err

PUSHD NotAllLoaded
CALL NotAllLoaded.bat
POPD
IF ERRORLEVEL 1 GOTO err

PUSHD CodePropIndex2533
CALL CodePropIndex2533.bat
POPD
IF ERRORLEVEL 1 GOTO err

PUSHD CommitHooksTest
CALL CommitHooksTest.bat
POPD
IF ERRORLEVEL 1 GOTO err

PUSHD TestIApplicationHost
CALL AppHostTest.bat
POPD
IF ERRORLEVEL 1 GOTO err

PUSHD OffsetkeyBug2915
CALL OffsetkeyBug2915.bat
POPD
IF ERRORLEVEL 1 GOTO err

:: Renaming back temporary directories.
PUSHD "%StarcounterBin%"
IF EXIST DontUseLibrariesWithDatabaseClasses ( RENAME DontUseLibrariesWithDatabaseClasses LibrariesWithDatabaseClasses )
IF EXIST DontUseEditionLibraries ( RENAME DontUseEditionLibraries EditionLibraries )
POPD

ECHO Regression test of simple apps succeeded.
EXIT /b 0


:err
ECHO Error:  Regression test of simple apps failed!

:: Renaming back temporary directories.
PUSHD "%StarcounterBin%"
IF EXIST DontUseLibrariesWithDatabaseClasses ( RENAME DontUseLibrariesWithDatabaseClasses LibrariesWithDatabaseClasses )
IF EXIST DontUseEditionLibraries ( RENAME DontUseEditionLibraries EditionLibraries )
POPD

EXIT /b 1
