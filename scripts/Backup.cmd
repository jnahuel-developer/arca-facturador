@echo off
setlocal
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Backup-ArcaFacturadorData.ps1" %*
if errorlevel 1 (
  echo.
  echo El backup fallo. Revise el mensaje anterior.
  pause
  exit /b 1
)
echo.
echo Backup finalizado.
pause
