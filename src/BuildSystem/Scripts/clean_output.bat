:: Checking if we are in Level1/bin/X.
IF NOT EXIST "scnetworkgateway.exe" GOTO :EOF

:: Killing Starcounter-related processes (including build processes) if any are running.
CALL "kill_all.bat"

:: Removing disturbing directories.
RMDIR ".db" /S /Q
RMDIR ".db.output" /S /Q
RMDIR ".srv" /S /Q