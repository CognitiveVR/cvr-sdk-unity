@ECHO off
@SET BASEDIR=%cd%
@ECHO Making cognitiveVR package...
@ECHO %BASEDIR%
@ECHO Builder.MakeCognitiveVRPackage
"C:\Program Files\Unity\Editor\Unity" -batchmode -quit -projectPath "%BASEDIR%\..\CognitiveVRUnity" -executeMethod Builder.MakeCognitiveVRPackage
@ECHO Builder.MakeBPPackage
"C:\Program Files\Unity\Editor\Unity" -batchmode -quit -projectPath "%BASEDIR%\..\examples\BubblePop" -executeMethod  Builder.MakeBPPackage
@ECHO Done.
@PAUSE
