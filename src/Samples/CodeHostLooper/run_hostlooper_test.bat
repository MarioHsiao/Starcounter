:: Checking if test should be run.
IF "%SC_RUN_HOST_LOOPER_TEST%"=="False" GOTO :EOF

:: Killing all processes.
staradmin kill all

:: Starting NetworkIoTest in background.
START CMD /C "star.exe --nodb s\NetworkIoTest\NetworkIoTest.exe DbNumber=1 PortNumber=8080 TestType=MODE_NODE_TESTS"

:: Starting the client part of the test.
CodeHostLooper.exe

:: Checking exit code.
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

:: Success message.
ECHO Host looper test finished successfully!

:: Killing all processes.
staradmin kill all
GOTO :EOF

:: If we are here than some test has failed.
:TESTFAILED
ECHO Error occurred during the test! 1>&2
staradmin kill all
EXIT 1