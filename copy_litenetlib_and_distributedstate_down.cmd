pushd c:\git\Holofunk3
rmdir /s /q HoloLens2\Holofunk_HL2\Assets\LiteNetLib
rmdir /s /q HoloLens2\Holofunk_HL2\Assets\DistributedStateLib
robocopy /s ..\LiteNetLib\LiteNetLib HoloLens2\Holofunk_HL2\Assets\LiteNetLib *.cs
robocopy /s ..\DistributedState\DistributedStateLib HoloLens2\Holofunk_HL2\Assets\DistributedStateLib *.cs

rem Not sure these are needed at all...
rem rmdir /s /q HoloLens2\Holofunk_HL2\Assets\LiteNetLib\obj
rem rmdir /s /q HoloLens2\Holofunk_HL2\Assets\DistributedState\DistributedStateLib\obj
rem rmdir /s /q HoloLens2\Holofunk_HL2\Assets\DistributedState\DistributedStateTest
rem rmdir /s /q HoloLens2\Holofunk_HL2\Assets\DistributedState\DistributedThing\obj

