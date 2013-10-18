#!/bin/bash
# Copyright © 2013, Elián Hanisch
#
# This script is for packaging everything into a zip file.

NAME="RCSBuildAid"
DIR="Package/$NAME"

# Get plugin version
VERSION="$(grep AssemblyVersion AssemblyInfo.cs)"
VERSION=${VERSION/*AssemblyVersion(\"/}
VERSION=${VERSION/.\*\")*/}

rm -rf "$DIR"
mkdir -vp "$DIR"
mkdir -vp "$DIR/Plugins"
mkdir -vp "$DIR/Sources"

cp -v "bin/Release/$NAME.dll" "$DIR/Plugins"
cp -v *.cs "$DIR/Sources"
cp -v *.txt "$DIR"
cp -v *.asciidoc "$DIR"

cd Package
ZIPNAME="${NAME}_v${VERSION}.zip"
rm -f "$ZIPNAME"
zip -r "$ZIPNAME" "$NAME"
