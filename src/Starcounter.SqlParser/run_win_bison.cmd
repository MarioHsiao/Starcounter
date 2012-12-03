echo %1
cd %1
echo %cd%
echo %teamcity.build.checkoutDir%
win_bison.exe -d %2 -o %3
