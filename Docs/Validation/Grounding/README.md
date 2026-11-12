# Loaded vehicle grounding verification

Unity 6000.6.2f1, native Linux editor with Vulkan. 18 passed, 0 failed.

Actual city road and a dynamic wheel-collider player; six passengers board before driving. Cases include acceleration, hard turns, held braking, reverse, a staged 12 cm kerb and a stopped kinematic traffic-body collision. Existing traffic and rivals remain present. Individual cases are staged at the start location; this is not a complete human shift playtest.

Parking applies torque to all four wheels without requesting reverse gear.

Minimum upright dot: 0.999999. Maximum speed: 55.33 km/h. Longest airborne interval: 0.000 s. Wheel mount drift: 0.000000 m. Peak motor torque: 3354.8 Nm. Recovery teleports: 0.

Before-fix evidence is retained separately. The old setup allowed helpers to write roll despite physics constraints, changed centre of mass at runtime, and let collision damage displace or detach wheel colliders. Current settings remove those conflicts.

These grounding results were captured before the subsequent higher-speed powertrain tune. Current speed/gear/audio checks are recorded separately in ../Powertrain.
