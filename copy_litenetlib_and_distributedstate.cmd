rmdir /s /q HoloLens2\Holofunk_HL2\Assets\LiteNetLib
rmdir /s /q HoloLens2\Holofunk_HL2\\Assets\DistributedState
robocopy /s ..\LiteNetLib\LiteNetLib HoloLens2\Holofunk_HL2\Assets\LiteNetLib *.cs
robocopy /s ..\DistributedStateTest HoloLens2\Holofunk_HL2\Assets\DistributedState *.cs
rmdir /s /q HoloLens2\Holofunk_HL2\Assets\LiteNetLib\obj
rmdir /s /q HoloLens2\Holofunk_HL2\Assets\DistributedState\DistributedStateLib\obj
rmdir /s /q HoloLens2\Holofunk_HL2\Assets\DistributedState\DistributedStateTest
rmdir /s /q HoloLens2\Holofunk_HL2\Assets\DistributedState\DistributedThing\obj

