@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Uninstall-ArcaFacturador.ps1" %*
if errorlevel 1 (
  echo.
  echo La desinstalacion fallo. Revise el mensaje anterior.
  pause
  exit /b 1
)
echo.
echo Desinstalacion finalizada.
pause
