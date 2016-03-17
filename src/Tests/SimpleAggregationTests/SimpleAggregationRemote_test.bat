CMD /C "star.exe AggregationTestServer.cs"

IF %ERRORLEVEL% NEQ 0 GOTO :EOF

:: Checking if Debug or Release.
SET TEST_DIR=AggregationTestClient\bin\Release
IF NOT EXIST "%TEST_DIR%\AggregationTestClient.exe" SET TEST_DIR=AggregationTestClient\bin\Debug

CMD /C "RunRemoteClients.exe --MaxNumClients=3 --ExeToStart=%TEST_DIR%\AggregationTestClient_test.exe --ExeParams="--ServerAddress=%COMPUTERNAME%" --MaxWaitTimeSeconds=60"

IF %ERRORLEVEL% NEQ 0 GOTO :EOF