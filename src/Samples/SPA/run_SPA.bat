@echo off
set current=%CD%
cd %StarcounterBin%\s\SPA
%StarcounterBin%\star.exe SPA.exe
::IF %ERRORLEVEL% NEQ 0 (
PAUSE
::)
cd %current%