@echo off
chcp 65001 > nul
cd /d "%~dp0"
powershell -ExecutionPolicy Bypass -File "run.ps1" 2>nul || type "run.ps1" | powershell -
