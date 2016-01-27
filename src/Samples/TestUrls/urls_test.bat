:: Checking if test should be run.
IF "%SC_RUN_URLS_TEST%"=="False" GOTO :EOF

:: Killing all processes.
staradmin kill all

:: Starting Starcounter in background.
star "%StarcounterBin%\s\NetworkIoTest\NetworkIoTest.exe"

:: Starting the client part of the test.
TestUrls.exe

:: Checking exit code.
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

:: Success message.
ECHO URL test finished successfully!

:: Killing all processes.
staradmin kill all
GOTO :EOF

:: If we are here than some test has failed.
:TESTFAILED
ECHO Error occurred during the test! 1>&2
staradmin kill all
EXIT /b 1