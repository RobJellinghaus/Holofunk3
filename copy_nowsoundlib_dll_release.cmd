mkdir AzureKinect\Holofunk_AzKin\Assets\Plugins\x86_64
copy /y /v ..\NowSound\NowSoundLib\x64\Release\NowSoundLib.dll AzureKinect\Holofunk_AzKin\Assets\Plugins\x86_64
copy /y /v ..\NowSound\NowSoundLib\x64\Release\NowSoundLib.pdb AzureKinect\Holofunk_AzKin\Assets\Plugins\x86_64
mkdir AzureKinect\Holofunk_AzKin\Assets\NowSoundLib
copy /y /v ..\NowSound\NowSoundPInvokeLib\NowSoundLib.cs AzureKinect\Holofunk_AzKin\Assets\NowSoundLib
pause

