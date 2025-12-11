==================================================
             REALISTIC RAIN VFX - README
==================================================

Realistic Rain
Version: 1.0
Author: Roman CHACORNAC

INSTRUCTIONS:
-------------
1. Add one of the rain prefabs to your scene:
   - VFX_Realistic_Rain_Lit.prefab
   - VFX_Realistic_Rain_Unlit.prefab
   - Or any variant (NoSteam, NoCollision, Refraction, etc.)
2. Locate the parent GameObject (includes the controller script).
3. Attach it to your character controller or desired object for automatic positioning.

--------------------------------------------------

VFX CONTROLLER SCRIPT:
----------------------
Each prefab includes a controller script attached to its parent. 
This script allows real-time customization of the rain effect parameters.

FEATURES:
  * Color Control: Adjust rain tint dynamically with a color picker.
  * Particle Intensity: Control emission rate to increase or decrease rain density.
  * Wind Simulation: Apply directional velocity to simulate wind effects.

--------------------------------------------------

COMPATIBILITY:
--------------
- Unity Version: 2022.3.59f1 or later
- Render Pipelines: URP, HDRP, Built-in Pipeline
  * Note: Refraction effects require enabling "Opaque Texture" in Render Settings.

--------------------------------------------------

SUPPORT:
--------
For assistance or technical questions, contact:
optifx.fr@gmail.com

==================================================
