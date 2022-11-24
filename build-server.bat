@echo off
cd ./server
start /wait /b cmd "/C npm install"
echo Compiling...
start /wait /b cmd "/C npx tsc"
cd ..