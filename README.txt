RCSBuildAid KSP Plugin
======================
Elián Hanisch <lambdae2@gmail.com>
v0.1, June 2013:

Aid tool for the placement of RCS thrusters while building a rocket.

Requirements
------------

KSP version 0.20.* (0.19 and below not tested).

Installation
-----------

Just copy RCSBuildAid directory into your KSP's GameData directory. The .dll
path should be `KSP_path/GameData/RCSBuildAid/Plugins/RCSBuildAid.dll`.

Usage
-----

While in VAB (Vehicular Assembly Building) or in SPH (Space Plane Hangar) enable
Center of Mass (CoM) marker and press `space`, this should enable the displaying
of the forces exerted by the RCS thrusters (if you don't have any RCS attached
to your vessel you might see nothing though), pressing `space` again will cycle
between the 6 different directions: right, up, forward, left, down and
backwards, these are the directions your vessel can translate with the `ijkjlhn`
keys.

The forces displayed are 3:

* Thruster forces:
These are colored in cyan and represent the force exerted by the thruster for
move in a given direction.

* Translation force:
Colored in green, represents the translation motion of your vessel.

* Torque force:
Colored in red, represents the resulting torque the thrusters are exerting into
your vessel. When you see a red arrow, it means at in the current configuration,
your vessel will rotate when trying to translate at the given direction. Because
the CoM marker is always rendered on top and will obscure the arrow, when this
force is small enough the CoM marker will shrink, once it has completely
disappeared you know that the torque force is zero. If you want to know in what
direction your vessel will rotate, use the "right hand rule": put the thumb of
your right hand in the direction of the red arrow, your other fingers should
indicate the rotation motion.

When placing RCS thrusters you want them to be balanced around your CoM,
so your vessel won't rotate when translating in docking mode or with the
`ijklhn` keys, for this place the RCS parts making sure that there's no torque
(you shouldn't see the CoM marker nor a red arrow) then press `space` for check
the other directions, if there's no CoM marker in any of the 6 directions
the vessel should be balanced.

Notes
-----
* Be aware that consuming fuel will change your CoM, so a vessel that was
initially balanced will most likely start rotating when a sizable amount of
fuel was used.

* This plugin will work only with RCS thrusters that use the `ModuleRCS` module.

* The `space` key function is hardcoded and is also used for reset part rotation
in VAB/SPH, since this isn't a problem for me I haven't bothered rebinding it to
other key or making it configurable.

License
-------
This plugin is distributed under the terms of the LGPLv3.