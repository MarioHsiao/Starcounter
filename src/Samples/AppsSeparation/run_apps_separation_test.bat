:: Checking if test should be run.
IF "%SC_RUN_APPS_SEPARATION_TEST%"=="False" GOTO :EOF

IF "%MsbuildExe%"=="" SET MsbuildExe=C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe

IF "%Configuration%"=="" SET Configuration=Debug

:: Setting required variables.
IF "%SC_CHECKOUT_DIR%"=="" SET SC_CHECKOUT_DIR=%cd%

:: Killing existing processes.
"%StarcounterBin%/staradmin.exe" kill all

:: Building solution.
ECHO Building solution.
"%MsbuildExe%" "%SC_CHECKOUT_DIR%\Level1\src\Samples\AppsSeparation\App1\App1.sln" /p:ReferencePath="%StarcounterBin% /p:Configuration=%Configuration%
IF ERRORLEVEL 1 GOTO FAILED

:: Starting first App
ECHO Starting App1
star.exe "%SC_CHECKOUT_DIR%\Level1\src\Samples\AppsSeparation\App1\bin\%Configuration%\App1.exe"
IF ERRORLEVEL 1 GOTO FAILED

:: Starting second App
ECHO Starting App2
star.exe "%SC_CHECKOUT_DIR%\Level1\src\Samples\AppsSeparation\App2\bin\%Configuration%\App2.exe"
IF ERRORLEVEL 1 GOTO FAILED

ECHO Re-Starting App2
star.exe "%SC_CHECKOUT_DIR%\Level1\src\Samples\AppsSeparation\App2\bin\%Configuration%\App2.exe"
IF ERRORLEVEL 1 GOTO FAILED

ECHO Re-Starting App1
star.exe "%SC_CHECKOUT_DIR%\Level1\src\Samples\AppsSeparation\App1\bin\%Configuration%\App1.exe"
IF ERRORLEVEL 1 GOTO FAILED

ECHO Deleting database default

staradmin --database=default stop db
IF ERRORLEVEL 1 GOTO FAILED

staradmin --database=default delete --force db
IF ERRORLEVEL 1 GOTO FAILED

ECHO Apps Separation test finished successfully!

"%StarcounterBin%/staradmin.exe" kill all

GOTO :EOF

:: If we are here than some test has failed.
:FAILED

SET EXITCODE=%ERRORLEVEL%

:: Ending sequence.
POPD
"%StarcounterBin%/staradmin.exe" kill all
ECHO Exiting build with error code %EXITCODE%!
EXIT /b %EXITCODE%
