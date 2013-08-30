RCSBuildAid KSP Plugin
======================
Elián Hanisch <lambdae2@gmail.com>
v0.3.2, August 2013:

Aid tool for balancing RCS thrusters around the center of mass while building a
rocket.

Requirements
------------

KSP version 0.21.*

Installation
------------

Just copy RCSBuildAid directory into your KSP's GameData directory. The .dll
path should be `KSP_path/GameData/RCSBuildAid/Plugins/RCSBuildAid.dll`.

Features
--------

* Dry center of mass marker (the center of mass if the vessel had no fuel).
* Displaying of translation and torque forces due to RCS thrusters, this helps
  in placing RCS thrusters in balanced positions around the center of mass.
* Basic display of torque forces due to engines.

Usage
-----

While in VAB (Vehicular Assembly Building) or in SPH (Space Plane Hangar) turn 
on the Center of Mass (CoM) marker. You should see an extra red marker close to 
the CoM marker, this is the Dry Center of Mass (DCoM) marker.

For display the RCS forces, use the translate flight controls. For disable 
everything just turn off the CoM marker.

Controls
~~~~~~~~

The controls use are the same of the translation flight controls, with the 
default game settings:

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

NOTE: Setting the same direction twice will disable the plugin.

M : Alternate between displaying forces in the CoM or DCoM.
P : Toggling between displaying RCS or engine forces.

NOTE: M and P keys are hardcoded and can't be rebinded for the time being.

Dry Center of Mass (DCoM)
~~~~~~~~~~~~~~~~~~~~~~~~~

The DCoM is red and displayed whenever the CoM is enabled. The DCoM is always 
shown together with the CoM, there's no option for displaying them individually.
The DCoM indicates the position of where your vessel's center of mass will be
when you're out of propellant (liquid fuel, oxidizer and mono-propellant). 

Forces
~~~~~~

Once a movement or rotation direction is set, you should see the forces exerted
by the RCS thrusters (if you don't have any RCS attached to your vessel you 
will see nothing).

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
and with the given input your vessel will rotate. While in rotation mode it 
should have a red marker indicating the ideal direction.

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
possible.

Rotation mode
^^^^^^^^^^^^^

This mode is active when you use the `hnjlki` keys with shift. It works
the same than translation except that here you want to reduce the translation
vector (green arrow) while keeping the torque vector aligned with the red
marker. In most vessels you won't need this mode since balaced translation
implies balanced rotation, but this is not always true.

Balancing around the DCoM
^^^^^^^^^^^^^^^^^^^^^^^^^

The resulting translation and torque forces are, by default, indicated for the
CoM, which is your center of mass when your vessel is fully fueled. For switch
CoMs press the M key. Then you can see the translation and torque forces for 
when your vessel has no fuel mass. You can't have your RCS balanced around both
center of masses, balancing around the DCoM might be worth it if your vessel
docks while almost out of fuel, but in general you want to build your vessel in
a way that keeps both center of masses as close as possible.

Notes
-----

* Be aware that consuming fuel will change your CoM, so a vessel that was
initially balanced will most likely start rotating when a sizable amount of
fuel was used, use the M key for display the forces in the DCoM.

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
