@echo off
if exist "./server/dist/index.js" (
  echo Server already compiled
) else (
  echo Installing dependencies...
  call build-server.bat
)
echo Starting...
node ./server/dist/index.js