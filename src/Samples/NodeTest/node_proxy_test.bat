:: Checking if test should be run.
IF "%SC_RUN_NODE_TEST_PROXY%"=="False" GOTO :EOF

SET SERVER_DIR=.srv

:: Killing all processes.
staradmin kill all

:: Creating repository if it does not exist.
IF NOT EXIST "%SERVER_DIR%" GOTO TESTFAILED
COPY /Y scnetworkgateway_proxy_test.xml %SERVER_DIR%\personal\scnetworkgateway.xml

:: Starting service in background.
START CMD /C "scservice.exe"

:: Starting the client part of the test.
NodeTest.exe %*

:: Checking exit code.
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

:: Success message.
ECHO Test finished successfully!

:: Copying back the default gateway config.
COPY /Y scnetworkgateway.sample.xml %SERVER_DIR%\personal\scnetworkgateway.xml

:: Killing all processes.
staradmin kill all
GOTO :EOF

:: If we are here than some test has failed.
:TESTFAILED
ECHO Error occurred during the test! 1>&2
staradmin kill all

:: Copying back the default gateway config.
COPY /Y scnetworkgateway.sample.xml %SERVER_DIR%\personal\scnetworkgateway.xml

EXIT /b 1