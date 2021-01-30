Tank Config ReadMe
==================

Do not rely on other peoples configs and prefix our definitions with `KIT_`
prefix. Single tank resources are specified in the SUBTYPE directly, dual tank
reference B9_TANK_TYPE defined in `MixedLiquidTypes.cfg`.
Flight-switchable may not contains addedMass/addedCost declrarations.
Generate tailored configs for CT, CDT, RFC and generic ones for the rest.

There is also a hard-coded tank type called "Structural" which has zero mass and cost and no resources. It is the default tank type for any subtypes which do not have another one defined.

Docs:
https://github.com/blowfishpro/B9PartSwitch/wiki/ModuleB9PartSwitch
https://github.com/sarbian/ModuleManager/wiki/Module-Manager-Handbook
https://github.com/KSP-RO/ProceduralParts/wiki/Config-Parameter-Documentation

Part definition exports variable when it wants to be patched with fuel configs:

`RESOURCE {
  name = LiterVolume
  maxAmount = 1000
  TankType = ...
}`

TankType = Liquid
TankType = Double
TankType = Solid



Todo:

* IST - same transforms as CT&CC
* wetworks
* antimatter
* verify dry mass and cost of all touched parts
* capacitors
* disable certain configs when mods are present/absent

Generic tanks to configure:

* inline spherical
* big sphere tank
* inflatable inline
* K&K ContainerSystem
* wrapper
* radial spherical
* Hex (segment, core, structure)
* stock stack tanks
* stock airplane tanks
* stock ore and rcs tanks
* tanks from WarpPlugin

Flight switchable: CT, CC, wetworks, k&k, mk3

Discounted: (tbd)

WarpPlugin other tanks:
InterstellarGasTank-Wedge: ??
crashpad: gas airbag :P
RV*: similar looking radial tanks for various stuff
ChargedParticleTrap: nice model, transforms
toroidal: two similar antimatter tanks and a disk
PositronTanks[12]: two sizes of tank
AntimatterTrap: tank for antimatter
AntimatterTanks[12]: two size of tank
tanks on converters
