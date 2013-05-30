:: Checking cleanup variable.
IF NOT "%SC_CLEAN_OUTPUT%"=="True" EXIT 0

:: Checking if we are in Level1/bin/X.
IF NOT EXIST "scnetworkgateway.exe" GOTO :EOF

:: Killing Starcounter-related processes (including build processes) if any are running.
CALL "kill_all.bat"

:: Sleeping some time using ping.
ping -n 10 127.0.0.1 > nul

:: Removing disturbing directories.
RMDIR ".db" /S /Q
RMDIR ".db.output" /S /Q
RMDIR ".srv" /S /Q
RMDIR "NetworkIoTest" /S /Q
RMDIR "s" /S /Q

:: Diagnostics.
ECHO Build output cleaned up successfully!

:: Always succeeds.
IF "%SC_RUNNING_ON_BUILD_SERVER%"=="True" EXIT 0