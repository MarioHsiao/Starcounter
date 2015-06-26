@echo off

pushd %~dp0

echo Generating doxygen documentation
doxygen Doxyfile

popd