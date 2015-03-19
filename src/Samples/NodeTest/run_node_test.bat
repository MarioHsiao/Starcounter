:: Checking if test should be run.
IF "%SC_RUN_NODE_TEST%"=="False" GOTO :EOF

:: Killing all processes.
staradmin kill all

:: Starting NetworkIoTest in background.
star.exe s\NetworkIoTest\NetworkIoTest.exe DbNumber=1 PortNumber=8080 TestType=MODE_NODE_TESTS

:: Starting the client part of the test.
NodeTest.exe %*

:: Checking exit code.
IF ERRORLEVEL 1 GOTO TESTFAILED

:: Success message.
ECHO Node tests finished successfully!

:: Killing all processes.
staradmin kill all
GOTO :EOF

:: If we are here than some test has failed.
:TESTFAILED
ECHO Error occurred during the Node test! 1>&2
staradmin kill all
EXIT /b 1