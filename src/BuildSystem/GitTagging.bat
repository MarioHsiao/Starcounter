::SET SC_CHECKOUT_DIR=c:\github\
::SET GIT_LEVEL1_BRANCH=develop
::SET GIT_LEVEL0_BRANCH=develop
::SET GIT_BUILDSYSTEM_BRANCH=develop
::SET SC_CHANNEL_NAME=daily
::SET BUILD_NUMBER=2.0.0.3

:: Path to git.exe
set GIT_PATH="c:\Program Files (x86)\Git\bin\git.exe"

:: Level1
cd "%SC_CHECKOUT_DIR%\Level1"
%GIT_PATH% tag -a "%GIT_LEVEL1_BRANCH%-%SC_CHANNEL_NAME%-%BUILD_NUMBER%" -m "Tag for %SC_CHANNEL_NAME% %GIT_LEVEL1_BRANCH% build %BUILD_NUMBER%"
%GIT_PATH% push origin "%GIT_LEVEL1_BRANCH%-%SC_CHANNEL_NAME%-%BUILD_NUMBER%"

:: Level0
cd "%SC_CHECKOUT_DIR%\Level0"
%GIT_PATH% tag -a "%GIT_LEVEL0_BRANCH%-%SC_CHANNEL_NAME%-%BUILD_NUMBER%" -m "Tag for %SC_CHANNEL_NAME% %GIT_LEVEL0_BRANCH% build %BUILD_NUMBER%"
%GIT_PATH% push origin "%GIT_LEVEL0_BRANCH%-%SC_CHANNEL_NAME%-%BUILD_NUMBER%"

:: BuildSystem
cd "%SC_CHECKOUT_DIR%\BuildSystem"
%GIT_PATH% tag -a "%GIT_BUILDSYSTEM_BRANCH%-%SC_CHANNEL_NAME%-%BUILD_NUMBER%" -m "Tag for %SC_CHANNEL_NAME% %GIT_BUILDSYSTEM_BRANCH% build %BUILD_NUMBER%"
%GIT_PATH% push origin "%GIT_BUILDSYSTEM_BRANCH%-%SC_CHANNEL_NAME%-%BUILD_NUMBER%"

::%GIT_PATH% tag -d TagName
::%GIT_PATH% push origin :refs/tags/TagName
