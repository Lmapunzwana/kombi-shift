# Native Unity menu and camera verification

22 passed; 0 failed. See menu-camera-results.json for individual checks.

Captured from Unity 6000.6.2f1 on Linux with Vulkan, using the actual Game view and normal dynamic wheel-collider physics during the driving sequence. Background traffic and rivals are disabled to isolate camera and vehicle behaviour. A brief synthetic roll/pitch test temporarily holds the body, then restores dynamic physics before driving. Camera orbit, zoom and menu actions are exercised through the same public methods called by their input handlers; this is not injected mouse-event testing or a human playtest.

The screenshots show the live title screen, focus state, controls/settings panels, chase/orbit/driver views and the post-driving view. Shelter pieces are hidden only while crossing the chase view.

Physics result: engine=True; rpm=1858; gear=0; controllerEnabled=True; startY=24.54; finalY=23.46; distance=17.95; up=1.00; recoveries=0
