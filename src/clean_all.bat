:: Checking if we are in Level1/src.
IF NOT EXIST "Level1.sln" GOTO :EOF

:: Killing Starcounter-related processes (including build processes) if any are running.
CALL "BuildSystem\Scripts\kill_all.bat"

:: Removing individual files.
DEL "Starcounter.Internal\Error.cs"
DEL "Starcounter.ErrorCodes\scerrres\scerrres.h"
DEL "Starcounter.ErrorCodes\ErrorCodeCompiler\Starcounter.Errors.dll"
DEL "Starcounter.ErrorCodes\ErrorCodeCompiler\Starcounter.Errors.pdb"

:: Cleaning whole Level0 solution.
SET DEVENV_EXE_PATH=C:\Program Files (x86)\Microsoft Visual Studio 11.0\Common7\IDE\devenv.com
"%DEVENV_EXE_PATH%" "..\..\Level0\msbuild\Blue.sln" /clean "Release|x64"
"%DEVENV_EXE_PATH%" "..\..\Level0\msbuild\Blue.sln" /clean "Debug|x64"
"%DEVENV_EXE_PATH%" "..\..\Level0\msbuild\Blue.sln" /clean "Release|Win32"
"%DEVENV_EXE_PATH%" "..\..\Level0\msbuild\Blue.sln" /clean "Debug|Win32"

:: IF EXIST ".nuget" RMDIR ".nuget" /S /Q
:: IF EXIST "packages" RMDIR "packages" /S /Q

:: Removing all Visual Studio temporary directories recursively.
FOR /D /R %%X IN (obj) DO IF EXIST "%%X" RMDIR "%%X" /S /Q
FOR /D /R %%X IN (bin) DO IF EXIST "%%X" RMDIR "%%X" /S /Q
FOR /D /R %%X IN (win32) DO IF EXIST "%%X" RMDIR "%%X" /S /Q
FOR /D /R %%X IN (x64) DO IF EXIST "%%X" RMDIR "%%X" /S /Q

:: Removing bin directory.
::RMDIR "..\bin" /S /Q