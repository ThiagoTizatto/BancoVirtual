#!/bin/bash
set -e

SRC="/mnt/c/Users/thiagots/Documents/desafio-banco/.claude/worktrees/hardening-producao"
DST="/home/thiagots/desafio-banco"

echo "=== SYNC: $SRC -> $DST ==="
rsync -a --delete \
    --exclude='.git/' \
    --exclude='**/bin/' \
    --exclude='**/obj/' \
    "$SRC/" "$DST/"

cd "$DST"
export PATH="/home/thiagots/.dotnet:/home/thiagots/.dotnet/tools:$PATH"
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

echo "=== BUILD ==="
dotnet build -warnaserror 2>&1
echo "BUILD_EXIT=$?"

echo "=== TESTS ==="
dotnet test --no-build 2>&1
echo "TEST_EXIT=$?"
