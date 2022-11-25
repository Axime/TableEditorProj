@echo off
setlocal EnableDelayedExpansion
if exist "./server/dist/index.js" (
  for /F "tokens=* usebackq" %%f in (`
    findstr /R "\"version\": \".+?\"," ".\server\package.json"
  `) do (
    set a=%%f
  )
  set current_version="!a:~12,-2!"
  for /F "tokens=* usebackq" %%f in (`
    findstr /R "\/\/ version:" ".\server\dist\index.js"
  `) do (
    set b=%%f
  )
  set needed_version="!b:~12!"
  echo Current version: !current_version:~1,-1!
  echo Last version: !needed_version:~1,-1!
  if "!current_version:~1,-1!" == "!needed_version:~1,-1!" (
    echo Server already compiled
    goto start
  )
  echo Server already compiled, but version is old
)
:build
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
set server_version=%__tmpvar:~12,-2%
cd ./dist
echo // version: %server_version%> index.js.tmp
type index.js >> index.js.tmp
move /y index.js.tmp index.js > nul
cd ..
cd ..
:start
echo Starting...
if "%1" == "-p" (
  set __PRODUCTION=1
  echo Production mode enabled
)
node ./server/dist/index.js