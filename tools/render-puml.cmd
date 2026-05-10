@echo off
setlocal

REM Render all PlantUML .puml files to images.
REM Default: render PNG via PlantUML public server, no local jar required.
REM Examples:
REM   tools\render-puml.cmd
REM   tools\render-puml.cmd -Format svg
REM   tools\render-puml.cmd -PlantUmlJar plantuml.jar

powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0render-puml.ps1" -UseServer %*
