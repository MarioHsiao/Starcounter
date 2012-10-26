:: Variables
set DocumentX=C:\Program Files (x86)\Innovasys\DocumentX2012\bin\DocumentXCommandLinex64.exe
set DocumentationProjectPath=InternalDoc
set DocumentationProjectName=SC.dxp
set DocumentationBuildConf=KostisBuildConfiguration

set SRVwebappsPath=\\192.168.8.14\Users\sctest\Desktop\webapps
set SRVusername=sctest
set SRVpassword=showme
set DocumentationParentFolder=Internal
set DocumentationResultFolder=build


:: Run DocumentX build
"%DocumentX%" ".\%DocumentationProjectPath%\%DocumentationProjectName%" [/buildconfiguration="%DocumentationBuildConf%"]
if %errorlevel% neq 0 goto FAILED


:: SCTESTSRV01
net use Q: %SRVwebappsPath% /user:%SRVusername% %SRVpassword%
if %errorlevel% neq 0 goto FAILED
rd /s /q "Q:\%DocumentationParentFolder%"
:: "rd" doesn't return errorlevel value
robocopy .\%DocumentationProjectPath%\%DocumentationResultFolder%\ Q: /e
if %errorlevel% gtr 8 goto FAILED
net use Q: /delete
if %errorlevel% neq 0 goto FAILED


:: Success
exit 0
:: Failed
:FAILED
exit 1