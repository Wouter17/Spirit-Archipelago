#!/bin/bash
# Based on https://stackoverflow.com/a/15885133/1683264
DIST_DIR="$(dirname "$0")"
GAME_DIR=""
STEAM_BEPINEX_VER="BepInEx_win_x64_5.4.23.4"

echo "Spirit Island Archipelago Mod Installer"
echo "script directory ${DIST_DIR}"

if [ ! -f "${DIST_DIR}//Archipelago.dll" ] ||
       [ ! -d "${DIST_DIR}//${STEAM_BEPINEX_VER}" ]; then
    echo "Corrupt installation package. If you are a developer, please refer to the README."
    exit 1
fi

echo "Please select your game executable..."
echo "Typically {mountpoint}/Steam/steamapps/common/Spirit Island/SpiritIsland.exe"

read -p "game executable (full location): " GAME_DIR
BEPINEX_DIR="${GAME_DIR}/BepInEx"
PLUGINS_DIR="${GAME_DIR}/BepInEx/plugins"
ASSETS_DIR="${GAME_DIR}/BepInEx/plugins/assets"

echo "${GAME_DIR}"
echo "${BEPINEX_DIR}"
echo "${ASSETS_DIR}"
sleep 3

echo "Installing 'Spirit Island Archipelago' into "${PLUGINS_DIR}""
echo

mkdir -p "${BEPINEX_DIR}"
mkdir -p "${PLUGINS_DIR}"
mkdir -p "${ASSETS_DIR}"

cp -r "${DIST_DIR}//${STEAM_BEPINEX_VER}/"* "${GAME_DIR}"

cp "${DIST_DIR}//"*.dll "${PLUGINS_DIR}"

cp -r "${DIST_DIR}//assets" "${ASSETS_DIR}"

cp "${DIST_DIR}//si-modding-uninstall.sh" "${GAME_DIR}"

cp "${DIST_DIR}//doorstop_config.ini" "${GAME_DIR}/doorstop_config.ini"

echo
echo Successfully installed 'Spirit Island Archipelago Mod'
echo "You may now close this window"
