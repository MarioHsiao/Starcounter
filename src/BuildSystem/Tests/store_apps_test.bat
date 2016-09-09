PUSHD %SC_CHECKOUT_DIR%

cd SignIn
star --database=PolyjuiceTestsDb "bin/%Configuration%/SignIn.exe"
IF ERRORLEVEL 1 GOTO FAILED
REM call npm install > NUL
REM call npm install mocha-teamcity-reporter > NUL
REM node node_modules\mocha\bin\mocha --reporter mocha-teamcity-reporter
IF ERRORLEVEL 1 GOTO FAILED
cd ..

cd Launcher
star --database=PolyjuiceTestsDb "bin/%Configuration%/Launcher.exe"
IF ERRORLEVEL 1 GOTO FAILED
call npm install > NUL
call npm install mocha-teamcity-reporter > NUL
node node_modules\mocha\bin\mocha --reporter mocha-teamcity-reporter
cd ..

cd Products
star --database=PolyjuiceTestsDb "bin/%Configuration%/Products.exe"
IF ERRORLEVEL 1 GOTO FAILED
call npm install > NUL
call npm install mocha-teamcity-reporter > NUL
node node_modules\mocha\bin\mocha --reporter mocha-teamcity-reporter
cd ..

cd Barcodes
star --database=PolyjuiceTestsDb "bin/%Configuration%/Barcodes.exe"
IF ERRORLEVEL 1 GOTO FAILED
call npm install > NUL
call npm install mocha-teamcity-reporter > NUL
node node_modules\mocha\bin\mocha --reporter mocha-teamcity-reporter
cd ..

cd Skyper
star --database=PolyjuiceTestsDb "bin/%Configuration%/Skyper.exe"
IF ERRORLEVEL 1 GOTO FAILED
call npm install > NUL
call npm install mocha-teamcity-reporter > NUL
node node_modules\mocha\bin\mocha --reporter mocha-teamcity-reporter
cd ..

cd Images
star --database=PolyjuiceTestsDb "bin/%Configuration%/Images.exe"
IF ERRORLEVEL 1 GOTO FAILED
REM call npm install > NUL
REM call npm install mocha-teamcity-reporter > NUL
REM node node_modules\mocha\bin\mocha --reporter mocha-teamcity-reporter
cd ..

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
