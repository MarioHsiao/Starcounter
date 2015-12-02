:: Checking if test should be run.
IF "%SC_RUN_UDP_TEST%"=="False" GOTO :EOF

:: Killing all processes.
staradmin kill all

:: Starting NetworkIoTest in background.
star.exe "%StarcounterBin%\s\NetworkIoTest\NetworkIoTest.exe" DbNumber=1 PortNumber=8080 TestType=MODE_NODE_TESTS

:: Doing the client part of the test.
UdpTest.exe --DatagramSize=10
IF ERRORLEVEL 1 GOTO TESTFAILED

UdpTest.exe --DatagramSize=100
IF ERRORLEVEL 1 GOTO TESTFAILED

UdpTest.exe --DatagramSize=1000
IF ERRORLEVEL 1 GOTO TESTFAILED

UdpTest.exe --DatagramSize=10000
IF ERRORLEVEL 1 GOTO TESTFAILED

UdpTest.exe --DatagramSize=30000
IF ERRORLEVEL 1 GOTO TESTFAILED

:: Success message.
ECHO UDP test finished successfully!

:: Killing all processes.
staradmin kill all
GOTO :EOF

:: If we are here than some test has failed.
:TESTFAILED
ECHO Error occurred during the test! 1>&2
staradmin kill all
EXIT /b 1