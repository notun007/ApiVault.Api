#!/usr/bin/env sh
set -eu
SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
ROOT=$(CDPATH= cd -- "$SCRIPT_DIR/.." && pwd)
python3 "$SCRIPT_DIR/validate_source.py"
dotnet restore "$ROOT/ApiVault.sln"
dotnet build "$ROOT/ApiVault.sln" --configuration Release --no-restore
