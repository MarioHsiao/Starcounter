:: Checking if test should be run.
IF "%SC_RUN_NODE_TEST%"=="False" GOTO :EOF

CMD /C "kill_all.bat" 2>NUL

:: Creating repository if it does not exist.
IF NOT EXIST ".srv" star.exe @@CreateRepo .srv

:: Setting StarcounterBin as current directory.
SET StarcounterBin %CD%

:: Starting service in background.
START CMD /C "scservice.exe"

:: Waiting for service to initialize.
ping -n 10 127.0.0.1 > nul

:: Starting NetworkIoTest in background.
START CMD /C "star.exe --nodb s\NetworkIoTest\NetworkIoTest.exe DbNumber=1 PortNumber=8080 TestType=MODE_NODE_TESTS"

:: Waiting for test to initialize.
ping -n 40 127.0.0.1 > nul

:: Starting the client part of the test.
NodeTest.exe

:: Checking exit code.
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

:: Success message.
ECHO Node tests finished successfully!

CMD /C "kill_all.bat" 2>NUL
GOTO :EOF

:: If we are here than some test has failed.
:TESTFAILED
ECHO Error occurred during the Node test! 1>&2
CMD /C "kill_all.bat" 2>NUL
EXIT 1