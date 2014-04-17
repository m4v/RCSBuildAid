#!/bin/bash
# Copyright © 2013-2014, Elián Hanisch
#
# This script is for packaging everything into a zip file.

NAME="RCSBuildAid"
DIR="Package/$NAME"

# Get plugin version
VERSION="$(grep AssemblyVersion Plugin/AssemblyInfo.cs)"
VERSION=${VERSION/*AssemblyVersion(\"/}
VERSION=${VERSION/.\*\")*/}

rm -rf "$DIR"
mkdir -vp "$DIR"
mkdir -vp "$DIR/Plugins"
mkdir -vp "$DIR/Sources"
mkdir -vp "$DIR/Textures"

cp -v "Plugin/bin/Release/$NAME.dll" "$DIR/Plugins"
cp -v Plugin/*.cs "$DIR/Sources"
cp -v Textures/*.png "$DIR/Textures"
#cp -v *.txt "$DIR"
cp -v *.asciidoc "$DIR"

# Toolbar dll
mkdir -vp "$DIR/Sources/${NAME}Toolbar"
cp -v "${NAME}Toolbar/bin/Release/${NAME}Toolbar.dll" "$DIR/Plugins"
cp -v "${NAME}Toolbar"/*.cs "$DIR/Sources/${NAME}Toolbar"

cd Package
ZIPNAME="${NAME}_v${VERSION}.zip"
rm -f "$ZIPNAME"
zip -r "$ZIPNAME" "$NAME"

echo "Package ${ZIPNAME} built."


