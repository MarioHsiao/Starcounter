# level1/src/Starcounter.Installer/Starcounter.InstallerNativeWrapper/CMakeLists.txt

cmake_minimum_required(VERSION 2.8.10)

add_library(InstallerNativeWrapper SHARED
    Starcounter.InstallerNativeWrapper.cpp
    Starcounter.InstallerNativeWrapper.manifest
    resource.h
    Resource.rc
    resources/sc.ico
)
OrganizeVisualStudio(InstallerNativeWrapper) # "level1/Starcounter.Installer"

#  <ItemGroup>
#    <None Include="resources\dotnetfx45_full_x86_x64.exe" />
#    <None Include="resources\Starcounter-Setup.exe" />
#  </ItemGroup>