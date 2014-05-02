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

# copy plugins
mkdir -vp "$DIR/Plugins"
cp -v bin/Release/*.dll "$DIR/Plugins"

# copy sources
mkdir -vp "$DIR/Sources/GUI"
mkdir -vp "$DIR/Sources/${NAME}Toolbar"
cp -v Plugin/*.cs "$DIR/Sources"
cp -v Plugin/GUI/*.cs "$DIR/Sources/GUI"
cp -v "${NAME}Toolbar"/*.cs "$DIR/Sources/${NAME}Toolbar"

# copy documentation
cp -v *.asciidoc "$DIR"

# copy other files
mkdir -vp "$DIR/Textures"
cp -v Textures/*.png "$DIR/Textures"

# make package
cd Package
ZIPNAME="${NAME}_v${VERSION}.zip"
rm -f "$ZIPNAME"
zip -r "$ZIPNAME" "$NAME"

echo "Package ${ZIPNAME} built."

