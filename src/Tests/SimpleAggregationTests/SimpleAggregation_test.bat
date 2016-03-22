CMD /C "star.exe AggregationTestServer.cs"

IF %ERRORLEVEL% NEQ 0 EXIT /b 1

:: Checking if Debug or Release.
SET TEST_DIR=AggregationTestClient\bin\Release
IF NOT EXIST "%TEST_DIR%\AggregationTestClient.exe" SET TEST_DIR=AggregationTestClient\bin\Debug

CMD /C "%TEST_DIR%\AggregationTestClient.exe"

IF %ERRORLEVEL% NEQ 0 EXIT /b 1