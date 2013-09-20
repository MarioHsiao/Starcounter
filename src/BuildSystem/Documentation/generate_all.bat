set DocXPath=C:\Program Files (x86)\Innovasys\DocumentX2012\bin\DocumentXCommandLinex64.exe

call "%DocXPath%" ".\public\starcounter.dxp" [/buildconfiguration="Public"]
call "%DocXPath%" ".\internal\starcounter.dxp" [/buildconfiguration="Internal"]
