set DBNAME=DbMappingTestsDb

:: Make sure we are indifferent to if installation directory contain
:: a trailing slash or not
SET installationDir=%StarcounterBin%
IF %installationDir:~-1%==\ SET installationDir=%installationDir:~0,-1%

staradmin start server
staradmin --database=%DBNAME% delete --force db
IF %ERRORLEVEL% NEQ 0 GOTO FAILED

:: Starting server application.
star.exe --database=%DBNAME% "%installationDir%\s\ExtensionsTests\StarcounterExtensionsTests.exe"
IF %ERRORLEVEL% NEQ 0 GOTO FAILED

staradmin --database=%DBNAME% stop db
staradmin --database=%DBNAME% delete --force db

ECHO Database mapping test finished successfully!
EXIT /b 0

:: If we are here than some test has failed.
:FAILED
ECHO Exiting test with error code %EXITCODE%!
EXIT /b 1
