@echo off

if not exist SpiritIsland.exe echo Error: Uninstall script is not in game folder
if not exist SpiritIsland.exe pause
if not exist SpiritIsland.exe exit 1

del /f /q changelog.txt 
del /f /q winhttp.dll
del /f /q doorstop_config.ini
del /f /q SIModding.json
del /f /q output_log.txt
del /f /q /s BepInEx
rmdir BepInEx /S /Q
del "%~f0"