pushd c:\git\Holofunk3
rmdir /s /q AzureKinect\Holofunk_AzKin\Assets\LiteNetLib
rmdir /s /q AzureKinect\Holofunk_AzKin\Assets\DistributedStateLib
rmdir /s /q HoloLens2\Holofunk_HL2\Assets\LiteNetLib
rmdir /s /q HoloLens2\Holofunk_HL2\Assets\DistributedStateLib

robocopy /s ..\LiteNetLib\LiteNetLib AzureKinect\Holofunk_AzKin\Assets\LiteNetLib *.cs
robocopy /s ..\DistributedState\DistributedStateLib AzureKinect\Holofunk_AzKin\Assets\DistributedStateLib *.cs
robocopy /s ..\LiteNetLib\LiteNetLib HoloLens2\Holofunk_HL2\Assets\LiteNetLib *.cs
robocopy /s ..\DistributedState\DistributedStateLib HoloLens2\Holofunk_HL2\Assets\DistributedStateLib *.cs

rmdir /s /q AzureKinect\Holofunk_AzKin\Assets\LiteNetLib\obj
rmdir /s /q AzureKinect\Holofunk_AzKin\Assets\DistributedStateLib\obj
rmdir /s /q HoloLens2\Holofunk_HL2\Assets\LiteNetLib\obj
rmdir /s /q HoloLens2\Holofunk_HL2\Assets\DistributedStateLib\obj

