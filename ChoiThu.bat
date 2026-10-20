@echo off
rem Bam dup de build game thanh ban Windows roi mo len choi thu (Unity Editor phai dang tat).
rem Them tham so: ChoiThu.bat -Rebuild (dung lai scene)  |  ChoiThu.bat -SkipBuild (mo ban cu)
chcp 65001 >nul
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\choi-thu.ps1" %*
if errorlevel 1 pause
