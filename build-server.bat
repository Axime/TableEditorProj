@echo off
setlocal EnableDelayedExpansion
cd ./server
echo Installing dependencies...
start /wait /b cmd "/C npm install"
echo Compiling...
start /wait /b cmd "/C npx tsc"
for /F "tokens=* usebackq" %%f in (`
  findstr /R "\"version\": \".+?\"," ".\package.json"
`) do (
  set __tmpvar=%%f
)
set server_version=!__tmpvar:~12,-2!
cd ./dist
echo // version: %server_version%> index.js.tmp
type index.js >> index.js.tmp
move /y index.js.tmp index.js > nul
cd ..
cd ..