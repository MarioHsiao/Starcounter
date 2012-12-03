echo %1
cd ..\packages
echo %cd%
cd %1
echo %cd%
echo %SC_CHECKOUT_DIR%
win_bison.exe -d %2 -o %3
