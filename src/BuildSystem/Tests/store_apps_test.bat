PUSHD %SC_CHECKOUT_DIR%\PolyjuiceApps

cd SignIn
ECHO Building SignIn
"%MsbuildExe%" SignIn.sln /p:ReferencePath="%StarcounterBin%;%StarcounterBin%/EditionLibraries;%StarcounterBin%/LibrariesWithDatabaseClasses" /p:Configuration=%Configuration% %MsBuildCommonParams%
IF ERRORLEVEL 1 GOTO FAILED
star --database=PolyjuiceTestsDb "bin/%Configuration%/SignIn.exe"
IF ERRORLEVEL 1 GOTO FAILED
REM call npm install > NUL
REM call npm install mocha-teamcity-reporter > NUL
REM node node_modules\mocha\bin\mocha --reporter mocha-teamcity-reporter
IF ERRORLEVEL 1 GOTO FAILED
cd ..

cd Launcher
ECHO Building Launcher
"%MsbuildExe%" Launcher.sln /p:ReferencePath="%StarcounterBin%;%StarcounterBin%/EditionLibraries;%StarcounterBin%/LibrariesWithDatabaseClasses" /p:Configuration=%Configuration% %MsBuildCommonParams%
IF ERRORLEVEL 1 GOTO FAILED
star --database=PolyjuiceTestsDb "bin/%Configuration%/Launcher.exe"
IF ERRORLEVEL 1 GOTO FAILED
call npm install > NUL
call npm install mocha-teamcity-reporter > NUL
node node_modules\mocha\bin\mocha --reporter mocha-teamcity-reporter
cd ..

cd Simplified
ECHO Building Simplified
"%MsbuildExe%" Simplified.sln /p:ReferencePath="%StarcounterBin%;%StarcounterBin%/EditionLibraries;%StarcounterBin%/LibrariesWithDatabaseClasses" /p:Configuration=%Configuration% %MsBuildCommonParams%
IF ERRORLEVEL 1 GOTO FAILED
cd ..

cd Products
ECHO Building Products
"%MsbuildExe%" Products.sln /p:ReferencePath="%StarcounterBin%;%StarcounterBin%/EditionLibraries;%StarcounterBin%/LibrariesWithDatabaseClasses" /p:Configuration=%Configuration% %MsBuildCommonParams%
IF ERRORLEVEL 1 GOTO FAILED
star --database=PolyjuiceTestsDb "bin/%Configuration%/Products.exe"
IF ERRORLEVEL 1 GOTO FAILED
call npm install > NUL
call npm install mocha-teamcity-reporter > NUL
node node_modules\mocha\bin\mocha --reporter mocha-teamcity-reporter
cd ..

cd Barcodes
ECHO Building Barcodes
"%MsbuildExe%" Barcodes.sln /p:ReferencePath="%StarcounterBin%;%StarcounterBin%/EditionLibraries;%StarcounterBin%/LibrariesWithDatabaseClasses" /p:Configuration=%Configuration% %MsBuildCommonParams%
IF ERRORLEVEL 1 GOTO FAILED
star --database=PolyjuiceTestsDb "bin/%Configuration%/Barcodes.exe"
IF ERRORLEVEL 1 GOTO FAILED
call npm install > NUL
call npm install mocha-teamcity-reporter > NUL
node node_modules\mocha\bin\mocha --reporter mocha-teamcity-reporter
cd ..

cd Procurement
ECHO Building Procurement
"%MsbuildExe%" Procurement.sln /p:ReferencePath="%StarcounterBin%;%StarcounterBin%/EditionLibraries;%StarcounterBin%/LibrariesWithDatabaseClasses" /p:Configuration=%Configuration% %MsBuildCommonParams%
IF ERRORLEVEL 1 GOTO FAILED
star --database=PolyjuiceTestsDb "bin/%Configuration%/Procurement.exe"
IF ERRORLEVEL 1 GOTO FAILED
call npm install > NUL
call npm install mocha-teamcity-reporter > NUL
node node_modules\mocha\bin\mocha --reporter mocha-teamcity-reporter
cd ..

cd Skyper
ECHO Building Skyper
"%MsbuildExe%" Skyper.sln /p:ReferencePath="%StarcounterBin%;%StarcounterBin%/EditionLibraries;%StarcounterBin%/LibrariesWithDatabaseClasses" /p:Configuration=%Configuration% %MsBuildCommonParams%
IF ERRORLEVEL 1 GOTO FAILED
star --database=PolyjuiceTestsDb "bin/%Configuration%/Skyper.exe"
IF ERRORLEVEL 1 GOTO FAILED
call npm install > NUL
call npm install mocha-teamcity-reporter > NUL
node node_modules\mocha\bin\mocha --reporter mocha-teamcity-reporter
cd ..

cd Images
ECHO Building Images
"%MsbuildExe%" Images.sln /p:ReferencePath="%StarcounterBin%;%StarcounterBin%/EditionLibraries;%StarcounterBin%/LibrariesWithDatabaseClasses" /p:Configuration=%Configuration% %MsBuildCommonParams%
IF ERRORLEVEL 1 GOTO FAILED
star --database=PolyjuiceTestsDb "bin/%Configuration%/Images.exe"
IF ERRORLEVEL 1 GOTO FAILED
REM call npm install > NUL
REM call npm install mocha-teamcity-reporter > NUL
REM node node_modules\mocha\bin\mocha --reporter mocha-teamcity-reporter
cd ..

REM cd Cartographer
REM ECHO Building Cartographer
REM "%MsbuildExe%" Cartographer.sln /p:ReferencePath="%StarcounterBin%;%StarcounterBin%/EditionLibraries;%StarcounterBin%/LibrariesWithDatabaseClasses" /p:Configuration=%Configuration% %MsBuildCommonParams%
REM IF ERRORLEVEL 1 GOTO FAILED
REM star --database=PolyjuiceTestsDb "bin/%Configuration%/Cartographer.exe"
REM IF ERRORLEVEL 1 GOTO FAILED
REM call npm install > NUL
REM call npm install mocha-teamcity-reporter > NUL
REM node node_modules\mocha\bin\mocha --reporter mocha-teamcity-reporter
REM cd ..

staradmin --database=PolyjuiceTestsDb stop db
staradmin --database=PolyjuiceTestsDb delete --force db
staradmin kill all

PUSHD

:: Build finished successfully.
GOTO :EOF

:: If we are here than some test has failed.
:FAILED

SET EXITCODE=%ERRORLEVEL%

:: Ending sequence.
IF EXIST "%StarcounterBin%/staradmin.exe" "%StarcounterBin%/staradmin.exe" kill all
ECHO Exiting Building with error code %EXITCODE%!
EXIT /b %EXITCODE%
