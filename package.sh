#!/bin/bash
# Copyright © 2013, Elián Hanisch
#
# This script is for packaging everything into a zip file.

NAME="RCSBuildAid"
DIR="Package/$NAME"

mkdir -vp "$DIR/Plugins"
mkdir -vp "$DIR/Sources"

cp -v "bin/Release/$NAME.dll" "$DIR/Plugins"
cp -v *.cs "$DIR/Sources"
cp -v "README.txt" "$DIR"

cd Package
zip -r "$NAME.zip" "$NAME"
