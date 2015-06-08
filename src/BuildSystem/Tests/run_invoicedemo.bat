
PUSHD "%SC_CHECKOUT_DIR%"

git clone https://github.com/Starcounter/InvoiceDemo.git

"%MsbuildExe%" "%SC_CHECKOUT_DIR%\InvoiceDemo\InvoiceDemo1.sln" /p:Configuration=%Configuration% %MsBuildCommonParams%
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED
star --database=InvoiceDemo1Db "%SC_CHECKOUT_DIR%\InvoiceDemo\bin\%Configuration%\InvoiceDemo.exe"
IF %ERRORLEVEL% NEQ 0 GOTO TESTFAILED

:: Success message.
ECHO InvoiceDemo tests finished successfully!

:: Killing all processes.
staradmin kill all

POPD
GOTO :EOF

:: If we are here than some test has failed.
:TESTFAILED
ECHO Error occurred during the InvoiceDemo test! 1>&2
staradmin kill all
EXIT /b 1