:: Variables
set DocumentX=C:\Program Files (x86)\Innovasys\DocumentX2012\bin\DocumentXCommandLinex64.exe
set DocumentationProjectPath=PublicDoc
set DocumentationProjectName=SC.dxp
set DocumentationBuildConf=PublicBuildConfiguration

set SRVwebappsPath=\\192.168.8.14\Users\sctest\Desktop\webapps
set SRVusername=sctest
set SRVpassword=showme
set DocumentationParentFolder=Public
set DocumentationResultFolder=build


:: Run DocumentX build
"%DocumentX%" ".\%DocumentationProjectPath%\%DocumentationProjectName%" [/buildconfiguration="%DocumentationBuildConf%"]
if %errorlevel% neq 0 goto FAILED


:: SCTESTSRV01
net use Q: %SRVwebappsPath% /user:%SRVusername% %SRVpassword%
rd /s /q "Q:\%DocumentationParentFolder%"
CMD /C "robocopy .\%DocumentationProjectPath%\%DocumentationResultFolder%\ Q: /e" 1>NUL
if %errorlevel% gtr 8 goto FAILED
net use Q: /delete


:: Success
exit 0
:: Failed
:FAILED
exit 1