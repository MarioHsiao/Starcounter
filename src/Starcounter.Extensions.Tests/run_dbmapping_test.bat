:: Checking if test should be run.
IF "%SC_RUN_DBMAPPING_TEST%"=="False" GOTO :EOF

:: Killing existing processes.
"%StarcounterBin%\staradmin.exe" kill all

:: Enabling database mapping.
SET SC_ENABLE_MAPPING=True

:: Starting server application.
star.exe --database=default --sc-codehostargs="--PolyjuiceDatabaseFlag=False" "%StarcounterBin%\s\ExtensionsTests\StarcounterExtensionsTests.exe"
IF ERRORLEVEL 1 GOTO FAILED

staradmin --database=default stop db
IF ERRORLEVEL 1 GOTO FAILED

staradmin --database=default delete --force db
IF ERRORLEVEL 1 GOTO FAILED

ECHO Database mapping test finished successfully!

:: Killing existing processes.
"%StarcounterBin%\staradmin.exe" kill all

GOTO :EOF

:: If we are here than some test has failed.
:FAILED

SET EXITCODE=%ERRORLEVEL%

:: Ending sequence.
"%StarcounterBin%/staradmin.exe" kill all
ECHO Exiting test with error code %EXITCODE%!
EXIT /b %EXITCODE%
