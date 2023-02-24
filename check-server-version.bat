@echo off
setlocal EnableDelayedExpansion
for /F "tokens=* usebackq" %%f in (`
  findstr /R "\"version\": \".+?\"," ".\server\package.json"
`) do (
  set a=%%f
)
set current_version=!a:~12,-2!
for /F "tokens=* usebackq" %%f in (`
  findstr /R "\/\/ version:" ".\server\dist\index.js"
`) do (
  set b=%%f
)
set needed_version=!b:~12!
echo Current version: %current_version%
echo Last version: %needed_version%
if "%current_version%" == "%needed_version%" (set compile=0) else (set compile=1)