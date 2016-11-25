
:: Make sure we are indifferent to if installation directory contain
:: a trailing slash or not
SET installationDir=%StarcounterBin%
IF %installationDir:~-1%==\ SET installationDir=%installationDir:~0,-1%

CMD /C "star.exe --sc-compilerefs="%installationDir%\Public Assemblies\Starcounter.Extensions.dll" AggregationTestServer.cs"

IF %ERRORLEVEL% NEQ 0 EXIT /b 1

PAUSE

:: Checking if Debug or Release.
SET TEST_DIR=AggregationTestClient\bin\Release
IF NOT EXIST "%TEST_DIR%\AggregationTestClient.exe" SET TEST_DIR=AggregationTestClient\bin\Debug

CMD /C "%TEST_DIR%\AggregationTestClient.exe"

IF %ERRORLEVEL% NEQ 0 EXIT /b 1