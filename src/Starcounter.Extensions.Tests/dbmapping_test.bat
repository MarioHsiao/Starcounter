set DBNAME=DbMappingTestsDb

staradmin start server
staradmin --database=%DBNAME% delete --force db
IF ERRORLEVEL 1 GOTO FAILED

:: Starting server application.
star.exe --database=%DBNAME% "%StarcounterBin%\s\ExtensionsTests\StarcounterExtensionsTests.exe"
IF ERRORLEVEL 1 GOTO FAILED

staradmin --database=%DBNAME% stop db
staradmin --database=%DBNAME% delete --force db

ECHO Database mapping test finished successfully!
GOTO :END

:: If we are here than some test has failed.
:FAILED
ECHO Exiting test with error code %EXITCODE%!

:END
:: Ending sequence.
"%StarcounterBin%/staradmin.exe" kill all