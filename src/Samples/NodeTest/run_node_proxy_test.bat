:: Checking if test should be run.
IF "%SC_RUN_NODE_TEST_PROXY%"=="False" GOTO :EOF

:: Killing all processes.
staradmin -killall

:: Creating repository if it does not exist.
IF NOT EXIST ".srv" star.exe @@CreateRepo .srv
COPY /Y scnetworkgateway_proxy_test.xml .srv\personal\scnetworkgateway.xml

:: Setting StarcounterBin as current directory.
SET StarcounterBin=%CD%

:: Starting service in background.
START CMD /C "scservice.exe"

:: Waiting for test to initialize.
ping -n 7 127.0.0.1 > nul

:: Starting the client part of the test.
NodeTest.exe %*

:: Checking exit code.
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

:: Success message.
ECHO Test finished successfully!

:: Killing all processes.
staradmin -killall
GOTO :EOF

:: If we are here than some test has failed.
:TESTFAILED
ECHO Error occurred during the test! 1>&2
staradmin -killall
EXIT 1