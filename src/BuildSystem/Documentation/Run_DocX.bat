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

:: SCTESTSRV01
net use Q: %SRVwebappsPath% /user:%SRVusername% %SRVpassword%
rd /s /q "Q:\%DocumentationParentFolder%"
robocopy .\%DocumentationProjectPath%\%DocumentationResultFolder%\ Q: /e
net use Q: /delete

exit 0