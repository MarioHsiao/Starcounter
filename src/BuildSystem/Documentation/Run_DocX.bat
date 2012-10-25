:: Variables
set DocumentxPath=C:\Program Files (x86)\Innovasys\DocumentX2012\bin
set DocumentationProjectPath=\InternalDoc
set DocumentationProjectName=SC.dxp
set DocumentationBuildConf=KostisBuildConfiguration

set SRVwebappsPath=\\192.168.8.14\Users\sctest\Desktop\webapps
set SRVusername=sctest
set SRVpassword=showme
set DocumentationParentFolder=Internal
set DocumentationResultFolder=build

:: Run DocumentX build
cd %DocumentxPath%
DocumentXCommandLinex64.exe "%DocumentationProjectPath%\%DocumentationProjectName%" [/buildconfiguration="%DocumentationBuildConf%"]

:: SCTESTSRV01
net use Z: %SRVwebappsPath% /user:%SRVusername% %SRVpassword%
rd /s /q "Z:\%DocumentationParentFolder%"
xcopy %DocumentationProjectPath%\%DocumentationResultFolder%\* Z: /s /i
net use Z: /delete
