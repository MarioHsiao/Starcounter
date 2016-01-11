:: Checking if test should be run.
IF "%SC_RUN_WEBSOCKETS_TEST%"=="False" GOTO :EOF

setlocal
for /f "tokens=4-5 delims=. " %%i in ('ver') do set WINVERSION=%%i.%%j
if "%WINVERSION%" LSS "6.2" (
	ECHO To run WebSockets test, the platform should be at least Windows 8.
	EXIT /b 0
)

:: Killing all processes.
staradmin kill all

:: Starting Starcounter in background.
star "%StarcounterBin%\s\WebSocketsTestServer\WebSocketsTestServer.exe"

:: Starting the client part of the test.
"%StarcounterBin%\WebSocketsTestClient.exe"

:: Checking exit code.
IF ERRORLEVEL 1 GOTO TESTFAILED

:: Starting the client part of the test.
"%StarcounterBin%\WebSocketsTestClient.exe" --MessageSizeBytes=1000

:: Checking exit code.
IF ERRORLEVEL 1 GOTO TESTFAILED

:: Starting the client part of the test.
"%StarcounterBin%\WebSocketsTestClient.exe" --MessageSizeBytes=10000

:: Checking exit code.
IF ERRORLEVEL 1 GOTO TESTFAILED

:: Starting the client part of the test.
"%StarcounterBin%\WebSocketsTestClient.exe" --MessageSizeBytes=100000

:: Checking exit code.
IF ERRORLEVEL 1 GOTO TESTFAILED

:: Success message.
ECHO WebSockets test finished successfully!

:: Killing all processes.
staradmin kill all
GOTO :EOF

:: If we are here than some test has failed.
:TESTFAILED
ECHO Error occurred during the test! 1>&2
staradmin kill all
EXIT /b 1