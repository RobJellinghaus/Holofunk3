pushd c:\git\Holofunk3
robocopy /s HolofunkCore AzureKinect\Holofunk_AzKin\Assets\HolofunkCore *.cs
robocopy /s HolofunkCore HoloLens2\Holofunk_HL2\Assets\HolofunkCore *.cs

rem ??? what are these for? Not sure where these obj directories would come from, leave it out for not
rem rmdir /s /q HoloLens2\Holofunk_HL2\Assets\LiteNetLib\obj
rem rmdir /s /q HoloLens2\Holofunk_HL2\Assets\DistributedState\DistributedStateLib\obj
rem rmdir /s /q HoloLens2\Holofunk_HL2\Assets\DistributedState\DistributedStateTest
rem rmdir /s /q HoloLens2\Holofunk_HL2\Assets\DistributedState\DistributedThing\obj

