:: Checking if test should be run.
IF "%SC_RUN_RETAILDEMO_TEST%"=="False" GOTO :EOF

:: Setting required variables.
IF "%SC_CHECKOUT_DIR%"=="" SET SC_CHECKOUT_DIR=%cd%

SET Path=%Path%;c:\Program Files (x86)\Git\bin
SET GIT_SSH=C:\Program Files (x86)\Atlassian\SourceTree\tools\putty\plink.exe
SET MsbuildExe=C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe

SET Configuration=Debug
SET RetailClientExe=RetailDemo\Client\bin\%Configuration%\RetailClient.exe
SET RetailServerExe=RetailDemo\Starcounter\bin\%Configuration%\ScRetailDemo.exe

:: Test parameters.
SET NumberOfCustomers=10000
SET NumberOfWorkers=4
SET NumberOfOperations=300000

:: Killing existing processes.
"%StarcounterBin%/staradmin.exe" kill all
IF EXIST "%SC_CHECKOUT_DIR%/RetailDemo" rd /q /s "%SC_CHECKOUT_DIR%/RetailDemo"

:: Pulling repository.

ECHO Cloning RetailDemo repository.
PUSHD "%SC_CHECKOUT_DIR%"
git clone https://github.com/Starcounter/RetailDemo.git --branch master RetailDemo
IF ERRORLEVEL 1 GOTO FAILED

:: Building repository.

ECHO Building RetailDemo solution.
"%MsbuildExe%" "RetailDemo/RetailDemo.sln" /p:Configuration=%Configuration%
IF ERRORLEVEL 1 GOTO FAILED

:: Starting server application.

star.exe %RetailServerExe%
IF ERRORLEVEL 1 GOTO FAILED

:: Using aggregation.

ECHO Inserting %NumberOfCustomers% objects using aggregation and %NumberOfWorkers% workers.
%RetailClientExe% -Inserting=True -UseAggregation=True -NumCustomers=%NumberOfCustomers% -NumTransferMoneyBetweenTwoAccounts=0 -NumGetCustomerAndAccounts=0 -NumGetCustomerById=0 -NumGetCustomerByFullName=0 -NumWorkersPerServerEndpoint=%NumberOfWorkers%
IF ERRORLEVEL 1 GOTO FAILED

ECHO Getting customers by ID using aggregation.
%RetailClientExe% -UseAggregation=True -NumCustomers=%NumberOfCustomers% -NumWorkersPerServerEndpoint=%NumberOfWorkers% -NumTransferMoneyBetweenTwoAccounts=0 -NumGetCustomerAndAccounts=0 -NumGetCustomerById=%NumberOfOperations% -NumGetCustomerByFullName=0
IF ERRORLEVEL 1 GOTO FAILED

ECHO Transfering money between accounts using aggregation.
%RetailClientExe% -UseAggregation=True -NumCustomers=%NumberOfCustomers% -NumWorkersPerServerEndpoint=%NumberOfWorkers% -NumTransferMoneyBetweenTwoAccounts=%NumberOfOperations% -NumGetCustomerAndAccounts=0 -NumGetCustomerById=0 -NumGetCustomerByFullName=0
IF ERRORLEVEL 1 GOTO FAILED

ECHO Mixed transactions using aggregation.
%RetailClientExe% -UseAggregation=True -NumCustomers=%NumberOfCustomers% -NumWorkersPerServerEndpoint=%NumberOfWorkers% -NumTransferMoneyBetweenTwoAccounts=%NumberOfOperations% -NumGetCustomerAndAccounts=%NumberOfOperations% -NumGetCustomerById=%NumberOfOperations% -NumGetCustomerByFullName=%NumberOfOperations%
IF ERRORLEVEL 1 GOTO FAILED


:: Using asyncronous Node.

ECHO Inserting %NumberOfCustomers% objects using asyncronous node and %NumberOfWorkers% workers.
%RetailClientExe% -Inserting=True -DoAsyncNode=True -NumCustomers=%NumberOfCustomers% -NumTransferMoneyBetweenTwoAccounts=0 -NumGetCustomerAndAccounts=0 -NumGetCustomerById=0 -NumGetCustomerByFullName=0  -NumWorkersPerServerEndpoint=%NumberOfWorkers%
IF ERRORLEVEL 1 GOTO FAILED

ECHO Getting customers by ID using asyncronous Node.
%RetailClientExe% -DoAsyncNode=True -NumCustomers=%NumberOfCustomers% -NumWorkersPerServerEndpoint=%NumberOfWorkers% -NumTransferMoneyBetweenTwoAccounts=0 -NumGetCustomerAndAccounts=0 -NumGetCustomerById=%NumberOfOperations% -NumGetCustomerByFullName=0
IF ERRORLEVEL 1 GOTO FAILED

ECHO Transfering money between accounts using asyncronous Node.
%RetailClientExe% -DoAsyncNode=True -NumCustomers=%NumberOfCustomers% -NumWorkersPerServerEndpoint=%NumberOfWorkers% -NumTransferMoneyBetweenTwoAccounts=%NumberOfOperations% -NumGetCustomerAndAccounts=0 -NumGetCustomerById=0 -NumGetCustomerByFullName=0
IF ERRORLEVEL 1 GOTO FAILED

ECHO Mixed transactions using asyncronous Node.
%RetailClientExe% -DoAsyncNode=True -NumCustomers=%NumberOfCustomers% -NumWorkersPerServerEndpoint=%NumberOfWorkers% -NumTransferMoneyBetweenTwoAccounts=%NumberOfOperations% -NumGetCustomerAndAccounts=%NumberOfOperations% -NumGetCustomerById=%NumberOfOperations% -NumGetCustomerByFullName=%NumberOfOperations%
IF ERRORLEVEL 1 GOTO FAILED


:: Using syncronous Node.

ECHO Inserting %NumberOfCustomers% objects using syncronous Node and %NumberOfWorkers% workers.
%RetailClientExe% -Inserting=True -NumCustomers=%NumberOfCustomers% -NumTransferMoneyBetweenTwoAccounts=0 -NumGetCustomerAndAccounts=0 -NumGetCustomerById=0 -NumGetCustomerByFullName=0  -NumWorkersPerServerEndpoint=%NumberOfWorkers%
IF ERRORLEVEL 1 GOTO FAILED

ECHO Getting customers by ID using syncronous Node.
%RetailClientExe% -NumCustomers=%NumberOfCustomers% -NumWorkersPerServerEndpoint=%NumberOfWorkers% -NumTransferMoneyBetweenTwoAccounts=0 -NumGetCustomerAndAccounts=0 -NumGetCustomerById=%NumberOfOperations% -NumGetCustomerByFullName=0
IF ERRORLEVEL 1 GOTO FAILED

ECHO Transfering money between accounts using syncronous Node.
%RetailClientExe% -NumCustomers=%NumberOfCustomers% -NumWorkersPerServerEndpoint=%NumberOfWorkers% -NumTransferMoneyBetweenTwoAccounts=%NumberOfOperations% -NumGetCustomerAndAccounts=0 -NumGetCustomerById=0 -NumGetCustomerByFullName=0
IF ERRORLEVEL 1 GOTO FAILED

ECHO Mixed transactions using syncronous Node.
%RetailClientExe% -NumCustomers=%NumberOfCustomers% -NumWorkersPerServerEndpoint=%NumberOfWorkers% -NumTransferMoneyBetweenTwoAccounts=%NumberOfOperations% -NumGetCustomerAndAccounts=%NumberOfOperations% -NumGetCustomerById=%NumberOfOperations% -NumGetCustomerByFullName=%NumberOfOperations%
IF ERRORLEVEL 1 GOTO FAILED

POPD

SET DB_NAME=default

ECHO Deleting database %DB_NAME%

staradmin --database=%DB_NAME% stop db
IF ERRORLEVEL 1 GOTO FAILED

staradmin --database=%DB_NAME% delete --force db
IF ERRORLEVEL 1 GOTO FAILED

:: Build finished successfully.
ECHO RetailDemo test finished successfully!

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
