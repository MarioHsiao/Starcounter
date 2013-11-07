:: Killing all Starcounter-related processes if any are running.
TASKKILL /f /t /im sccode.exe
TASKKILL /f /t /im scdata.exe
TASKKILL /f /t /im scdblog.exe
TASKKILL /f /t /im scnetworkgateway.exe
TASKKILL /f /t /im scnetworkgatewayloopedtest.exe
TASKKILL /f /t /im scipcmonitor.exe
TASKKILL /f /t /im scadminserver.exe
TASKKILL /f /t /im scweaver.exe
TASKKILL /f /t /im scsqlparser.exe

:: Sleeping some time using ping.
ping -n 3 127.0.0.1 > nul

::IF "%SC_RUNNING_ON_BUILD_SERVER%"=="True" EXIT 0