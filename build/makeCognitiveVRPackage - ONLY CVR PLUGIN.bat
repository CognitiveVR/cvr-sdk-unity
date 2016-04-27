@ECHO off
@SET BASEDIR=%cd%
@ECHO Making cognitiveVR package...
@ECHO %BASEDIR%
@ECHO Builder.MakeCognitiveVRPackage
"C:\Program Files\Unity\Editor\Unity" -batchmode -quit -projectPath "%BASEDIR%\..\CognitiveVRUnity" -executeMethod Builder.MakeCognitiveVRPackage


@ECHO Done.
@PAUSE
