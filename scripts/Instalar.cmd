@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Install-ArcaFacturador.ps1" %*
if errorlevel 1 (
  echo.
  echo La instalacion fallo. Revise el mensaje anterior.
  pause
  exit /b 1
)
echo.
echo Instalacion finalizada.
pause
