#!/bin/sh
set -eu

SETTINGS_FILE="/config/cfg/org.jdownloader.api.myjdownloader.MyJDownloaderSettings.json"

tmp_file="$(mktemp)"

jq \
  '.directconnectmode = "LAN_WAN_MANUAL"' \
  "$SETTINGS_FILE" > "$tmp_file"

mv "$tmp_file" "$SETTINGS_FILE"
