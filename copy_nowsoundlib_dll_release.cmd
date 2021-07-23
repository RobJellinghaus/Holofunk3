mkdir AzureKinect\Holofunk_AzKin\Assets\Plugins\x86_64
copy /y /v ..\NowSound\NowSoundLib\x64\Release\NowSoundLib.dll AzureKinect\Holofunk_AzKin\Assets\Plugins\x86_64
copy /y /v ..\NowSound\NowSoundLib\x64\Release\NowSoundLib.pdb AzureKinect\Holofunk_AzKin\Assets\Plugins\x86_64
mkdir AzureKinect\Holofunk_AzKin\Assets\NowSoundLib
copy /y /v ..\Holofunk2\Holofunk-Unity\Assets\Holofunk\NowSoundLib.cs AzureKinect\Holofunk_AzKin\Assets\NowSoundLib
mkdir HoloLens2\Holofunk_HL2\Assets\NowSoundLib
copy /y /v ..\Holofunk2\Holofunk-Unity\Assets\Holofunk\NowSoundLib.cs HoloLens2\Holofunk_HL2\Assets\NowSoundLib
rem copy /y /v ..\NowSound\NowSoundLib\x64\Release\NowSoundLib.dll Holofunk-PC-Mono
rem copy /y /v ..\NowSound\NowSoundLib\x64\Release\NowSoundLib.pdb Holofunk-PC-Mono
rem copy /y /v ..\NowSound\NowSoundLib\x64\Release\NowSoundLib.dll Holofunk-PC-Mono\build\bin\Holofunk-Unity_Data\Plugins
rem copy /y /v ..\NowSound\NowSoundLib\x64\Release\NowSoundLib.pdb Holofunk-PC-Mono\build\bin\Holofunk-Unity_Data\Plugins
pause

