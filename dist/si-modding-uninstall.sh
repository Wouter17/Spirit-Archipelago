#!/bin/bash

GAME_DIR="$(dirname "$0")"

if [ ! -f "${GAME_DIR}/SpiritIsland.exe" ]; then
    echo "Error: Uninstall script is not in game folder"
    exit 1
fi

rm "changelog.txt"
rm "winhttp.dll"
rm "doorstop_config.ini"
rm "SIModding.json"
rm "output_log.txt"
rm -r "BepInEx/"
rm -- "$0"
