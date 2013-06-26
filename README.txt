RCSBuildAid KSP Plugin
======================
Elián Hanisch <lambdae2@gmail.com>
v0.2, June 2013:

Aid tool for the placement of RCS thrusters while building a rocket.

Requirements
------------

KSP version 0.20.* (0.19 and below not tested).

Installation
------------

Just copy RCSBuildAid directory into your KSP's GameData directory. The .dll
path should be `KSP_path/GameData/RCSBuildAid/Plugins/RCSBuildAid.dll`.

Usage
-----

While in VAB (Vehicular Assembly Building) or in SPH (Space Plane Hangar) enable
Center of Mass (CoM) marker and use the translate flight controls, this should enable
the plugin. For disable it remove the CoM marker.

Controls
~~~~~~~~

The controls use are the same of the translation flight controls:

H : Set RCS forces to move forward.
N : Set RCS forces to move backwards.
L : Set RCS forces to translate right.
J : Set RCS forces to translate left.
I : Set RCS forces to translate up.
K : Set RCS forces to translate down.

Using shift will set a rotation motion instead of translation:

Shift + H : Set RCS forces to roll right.
Shift + N : Set RCS forces to roll left.
Shift + L : Set RCS forces to yaw right.
Shift + J : Set RCS forces to yaw left.
Shift + I : Set RCS forces to pitch up.
Shift + K : Set RCS forces to pitch down.

NOTE: Setting the same direction twice will disable the plugin an restore the
CoM marker.

Forces
~~~~~~

Once a movement or rotation direction is set, you should see the forces exerted
by the RCS thrusters (if you don't have any RCS attached to your vessel you
might see nothing though).

The forces displayed are of 3 types:

* Thruster forces:
These are colored in cyan and represent the force exerted by the RCS thruster
for move or rotate in a given direction.

* Translation force:
Colored in green, represents the translation motion of your vessel. While in
translation mode you should see a green marker near the tip of the arrow, this
indicates where it should be pointing ideally.

* Torque force:
Colored in red, represents the resulting torque the thrusters are exerting into
your vessel. When you see a red arrow, it means at in the current configuration
and with the given input your vessel will rotate. If you want to know in what
direction your vessel will rotate, use the "right hand rule": put the thumb of
your right hand in the direction of the red arrow, your other fingers should
indicate the rotation motion. Like the translation force, while in rotation
mode it should have a red marker indicating the ideal direction.

Balancing RCS
~~~~~~~~~~~~~

Having balanced RCS means that when you're translating your vessel won't rotate
and when you are rotating you won't translate, this is important for easy
docking. This depends of the position of your CoM and the placement of your
RCS thrusters.

Translation mode
^^^^^^^^^^^^^^^^

This mode is active when you use the `hnjlki` keys without shift. Here the
RCS will attempt to translate your vessel to the given direction, with the
green arrow being the actual resulting motion. In this situation you want
translation motion without any rotation, so you want to place your RCS around
your CoM in a way that reduces the torque vector (red arrow) as much as
possible. Because the CoM marker is always rendered on top and will obscure
anything behind it, when the torque force is small enough the CoM marker will
shrink, until it has completely disappeared meaning that your torque force is
zero. You should check that the CoM is small or nonexistent in all six
directions for your translation motion be balanced.

Rotation mode
^^^^^^^^^^^^^

This mode is active when you use the `hnjlki` keys with shift. It works
the same than translation except that here you want to reduce the translation
vector (green arrow) while keeping the torque vector aligned with the red
marker.

Notes
-----

* Be aware that consuming fuel will change your CoM, so a vessel that was
initially balanced will most likely start rotating when a sizable amount of
fuel was used.

* This plugin will work only with RCS thrusters that use the `ModuleRCS` module.

* In VAB/SPH the reference coordinate that this plugin uses is fixed to the
world space, while in flight your vessel coordinates are referenced to the
active command pod. So if you place your command pod in an odd direction what
you see in VAB/SPH won't match the experience in flight.

Reporting Bugs
--------------

You can report bugs or issues directly to GitHub:
https://github.com/m4v/RCSBuildAid/issues

Or in the forum thread in KSP Forums.

Links
-----

Repository in GitHub:
https://github.com/m4v/RCSBuildAid

License
-------

This plugin is distributed under the terms of the LGPLv3.
