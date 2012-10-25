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
net use Z: %SRVwebappsPath% /user:%SRVusername% %SRVpassword%
rd /s /q "Z:\%DocumentationParentFolder%"
robocopy .\%DocumentationProjectPath%\%DocumentationResultFolder%\ Z: /e
net use Z: /delete

exit 0