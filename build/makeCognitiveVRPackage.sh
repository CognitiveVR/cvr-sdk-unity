#!/bin/sh
pushd $(dirname "$0") >/dev/null
BASEDIR=$(pwd -P)
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath "$BASEDIR/../samples/BubblePop" -logFile /dev/stdout -executeMethod Builder.MakeSplytPackage
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath "$BASEDIR/../samples/BubblePop" -logFile /dev/stdout -executeMethod Builder.MakeBPPackage
popd >/dev/null
