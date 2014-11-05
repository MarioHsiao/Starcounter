set Configuration=Release

"C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe" "..\..\..\Level0\msbuild\SqlProcessor\SqlProcessor.sln" /property:Configuration=%Configuration%;Platform=x64;GenerateFullPaths=true /consoleloggerparameters:Summary /verbosity:minimal /maxcpucount

"C:\Windows\Microsoft.NET\Framework\v4.0.30319\msbuild.exe" "..\..\..\Level0\msbuild\Blue.sln" /property:Configuration=%Configuration%;Platform=x64;GenerateFullPaths=true /consoleloggerparameters:Summary /verbosity:minimal /maxcpucount
