#!/bin/bash
set -eu

VERSION=$1
PACKAGE_DIR=Packages/com.github.kurotu.faux-light-volumes
PACKAGE_JSON="$PACKAGE_DIR/package.json"

sed -i -b -e "s/\[Unreleased\]/\[${VERSION}\] - $(date -I)/g" CHANGELOG*.md
sed -i -b -e "s/\"version\": \".*\"/\"version\": \"${VERSION}\"/g" "$PACKAGE_JSON"

git add CHANGELOG*.md
git add "$PACKAGE_JSON"
git commit -m "Version $VERSION"
