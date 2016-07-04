#!/bin/sh
pushd $(dirname "$0") >/dev/null
BASEDIR=$(pwd -P)
/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath "$BASEDIR/../CognitiveVRUnity" -logFile /dev/stdout -executeMethod Builder.MakeCognitiveVRPackage
#/Applications/Unity/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath "$BASEDIR/../examples/BubblePop" -logFile /dev/stdout -executeMethod Builder.MakeBPPackage
popd >/dev/null
