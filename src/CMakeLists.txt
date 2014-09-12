# level1/src/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

add_definitions(-D_CRT_SECURE_NO_WARNINGS)

include_directories(Starcounter.ErrorCodes/scerrres)

add_subdirectory(Starcounter.ErrorCodes)
add_subdirectory(Chunks)
add_subdirectory(Hosting)
add_subdirectory(HTTP)
add_subdirectory(libs)
add_subdirectory(scnetworkgateway)
add_subdirectory(scservicelib)
add_subdirectory(Starcounter.Installer)
add_subdirectory(Utilities)

if(0)
set(SC_CSPROJ_LIST
    Browser/TestApp/TestApp.csproj
    BuildSystem/Build32BitComponents.csproj
    BuildSystem/CopyDependenciesBuildServer.csproj
    BuildSystem/CopyDependenciesLocal.csproj
    BuildSystem/CopyDependenciesRemote.csproj
    Development/Starcounter.VisualStudio/Starcounter.VisualStudio.csproj
    Development/Starcounter.VS.CSApplicationProjectTemplate/ApplicationProjectTemplate.csproj
    Development/Starcounter.VS.CSApplicationProjectTemplate/Starcounter.VS.CSApplicationProjectTemplate.csproj
    Development/Starcounter.VS.CSClassLibraryProjectTemplate/ClassLibraryProjectTemplate.csproj
    Development/Starcounter.VS.CSClassLibraryProjectTemplate/Starcounter.VS.CSClassLibraryProjectTemplate.csproj
    Development/Starcounter.VS.CSPuppetItemTemplate/Starcounter.VS.CSPuppetItemTemplate.csproj
    Development/Starcounter.VS.CSTypedJsonItemTemplate/Starcounter.VS.CSTypedJsonItemTemplate.csproj
    Development/Starcounter.VS.CSTypedJsonWithCodeItemTemplate/Starcounter.VS.CSTypedJsonWithCodeItemTemplate.csproj
    Development/Starcounter.VS.VSIX.2013/Starcounter.VS.VSIX.2013.csproj
    Development/Starcounter.VS.VSIX/Starcounter.VS.VSIX.csproj
    FasterThanJson/FasterThanJson.csproj
    FasterThanJson/FasterThanJson.Tests/FasterThanJson.Tests.csproj
    Hosting/Starcounter.Hosting/Starcounter.Hosting.csproj
    HTTP/HttpStructs.Tests/HttpParser.Tests.csproj
    Programs/Starcounter.TrayIcon/Starcounter.TrayIcon.csproj
    Programs/Starcounter.UsageTracker/Starcounter.UsageTracker.csproj
    Samples/AggrHttpClient/AggrHttpClient.csproj
    Samples/BuildSystem/BuildHelpers/BuildHelpers.csproj
    Samples/CycleTime/CycleTime.csproj
    Samples/hello/hello.csproj
    Samples/HelloGateway/HelloGateway.csproj
    Samples/IndexQueryTest/IndexQueryTest.csproj
    Samples/LoadAndLatency/LoadAndLatency.csproj
    Samples/MySampleApp/MySampleApp.csproj
    Samples/NetworkIoTest/NetworkIoTest.csproj
    Samples/NodeTest/NodeTest.csproj
    Samples/PolePosition/PolePosition.csproj
    Samples/QueryProcessingTest/QueryProcessingTest.csproj
    Samples/SqlCacheTrasher/SqlCacheTrasher.csproj
    Samples/SQLTest/SQLTest.csproj
    sccode/sccode.csproj
    scweaver.Test/scweaver.Test.csproj
    scweaver/scweaver.csproj
    Server/scadminserver/scadminserver.csproj
    Server/sccli/star.csproj
    Server/scservice/scservice.csproj
    Server/staradmin/staradmin.csproj
    Server/Starcounter.CLI/Starcounter.CLI.csproj
    Server/Starcounter.Server.Rest/Starcounter.Server.Rest.csproj
    Server/Starcounter.Server/Starcounter.Server.csproj
    Starcounter.Administrator/PickFileDialog/PickFileDialog.csproj
    Starcounter.Administrator/Starcounter.Administrator.csproj
    Starcounter.Apps.JsonPatch/Starcounter.App.Server.csproj
    Starcounter.Apps.Test/Starcounter.Apps.Tests.csproj
    Starcounter.BitsAndBytes.Test/BitsAndBytes.Test.csproj
    Starcounter.BitsAndBytes.Unsafe/BitsAndBytes.Unsafe.csproj
    Starcounter.BitsAndBytes/BitsAndBytes.csproj
    Starcounter.Bootstrap/Starcounter.Bootstrap.csproj
    Starcounter.ErrorCodes/ErrorCodeCompiler/scerrcc/scerrcc.csproj
    Starcounter.ErrorCodes/Starcounter.Errors/Starcounter.Errors.csproj
    Starcounter.Hypermedia/Starcounter.Hypermedia.csproj
    Starcounter.Installer/Starcounter.Controls.WpfMessageBox/Starcounter.Controls.WpfMessageBox.csproj
    Starcounter.Installer/Starcounter.InstallerEngine/Starcounter.InstallerEngine.csproj
    Starcounter.Installer/Starcounter.InstallerWPF/Starcounter.InstallerWPF.csproj
    Starcounter.Internal.Tests/Starcounter.Internal.Tests.csproj
    Starcounter.Internal/Starcounter.Internal.csproj
    Starcounter.Logging/Starcounter.Logging.csproj
    Starcounter.MsBuild/Starcounter.MsBuild.csproj
    Starcounter.Node/Starcounter.Node.csproj
    Starcounter.Rest/Starcounter.Rest.csproj
    Starcounter.SqlParser.Tests/Starcounter.SqlParser.Tests.csproj
    Starcounter.SqlProcessor.Tests/Starcounter.SqlProcessor.Tests.csproj
    Starcounter.Tests/Starcounter.Tests.csproj
    Starcounter.Tracking/Starcounter.Tracking.csproj
    Starcounter.Web.Tests/Starcounter.Web.Tests.csproj
    Starcounter.XSON.DeserializerCompiler/Starcounter.XSON.DeserializerCompiler.csproj
    Starcounter.XSON.HtmlReader.Tests/Starcounter.XSON.HtmlReader.Tests.csproj
    Starcounter.XSON.HtmlReader/Starcounter.XSON.HtmlReader.csproj
    Starcounter.XSON.Interfaces/Starcounter.XSON.Interfaces.csproj
    Starcounter.XSON.JsonByExample/Starcounter.XSON.JsonByExample.csproj
    Starcounter.XSON.JsonReader.Tests/Starcounter.XSON.JsonByExample.Tests.csproj
    Starcounter.XSON.JsonReader/Starcounter.XSON.JsonReader.csproj
    Starcounter.XSON.PartialClassGenerator.Tests/Starcounter.XSON.PartialClassGenerator.Tests.csproj
    Starcounter.XSON.PartialClassGenerator/Starcounter.XSON.PartialClassGenerator.csproj
    Starcounter.XSON.Tests/Starcounter.XSON.Tests.csproj
    Starcounter.XSON/Starcounter.XSON.csproj
    Starcounter/Starcounter.csproj
    ThirdParty/BizArkCore/BizArkCore.csproj
    ThirdParty/DynamoIoc/DynamoIoc.csproj
    ThirdParty/HtmlAgilityPack.Tests/HtmlAgilityPack.Tests.csproj
    ThirdParty/HtmlAgilityPack/HtmlAgilityPack.csproj
    ThirdParty/HtmlAgilityPack/HtmlAgilityPack\ VS2008.csproj
    ThirdParty/HtmlAgilityPack/HtmlAgilityPack.fx.4.0.csproj
    ThirdParty/HtmlAgilityPack/HtmlAgilityPack.fx.4.0-CP.csproj
    ThirdParty/HtmlAgilityPack/HtmlAgilityPack.fx.4.5.csproj
    ThirdParty/HtmlAgilityPack/HtmlAgilityPack.fx.4.5-CP.csproj
    ThirdParty/Newtonsoft.Json/Newtonsoft.Json.csproj
    Utilities/Sc.Tools.Logging/Sc.Tools.Logging.csproj
    Utilities/ServerLogTail/ServerLogTail.csproj
    Utilities/Starcounter.ClientResolver/Starcounter.ClientResolver.csproj
)

foreach(SC_CSPROJ_REL_PATH ${SC_CSPROJ_LIST})
    get_filename_component(SC_CSPROJ_FILENAME ${SC_CSPROJ_REL_PATH} NAME)
    string(REPLACE ".csproj" "" SC_CSPROJ_NAME ${SC_CSPROJ_FILENAME})
    # file(TO_NATIVE_PATH ${SC_CSPROJ_REL_PATH} SC_CSPROJ_REL_PATH_NATIVE)
    message("Adding C# project ${SC_CSPROJ_NAME}: \"${SC_CSPROJ_REL_PATH}\"")
    include_external_msproject(
        ${SC_CSPROJ_NAME}
        ${CMAKE_CURRENT_SOURCE_DIR}/${SC_CSPROJ_REL_PATH}
        TYPE FAE04EC0-301F-11D3-BF4B-00C04F79EFBC
    )
endforeach()
endif()
