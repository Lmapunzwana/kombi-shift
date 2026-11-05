# Kombi Shift — Project Direction and Development Plan

## Project overview

Kombi Shift is a school game project based on Zimbabwe's commuter-kombi experience. The player takes the role of a kombi driver operating through a busy city, collecting passengers, following a route, dealing with traffic and road hazards, and trying to complete a profitable shift.

The objective is not to create an enormous open-world simulator. We want to create a focused, polished vertical slice that demonstrates the complete gameplay experience convincingly.

The intended gameplay loop is:

```text
Main menu
→ Choose a vehicle or route
→ Begin a shift
→ Drive through the city
→ Collect passengers at designated stops
→ Navigate traffic, traffic lights and road hazards
→ Deliver passengers to their destinations
→ Earn fares and manage time, fuel and vehicle condition
→ Complete the shift and view the results
```

## What we already have

The project already contains a large city environment with:

- Buildings
- Pavements and sidewalks
- Street scenery
- Bus stops
- Environmental props
- A bus model
- Audio assets
- A route spline
- A basic main menu
- A standalone Linux build
- Initial vehicle, camera and road-hazard scripts

This means we do not need to create an entire city from scratch. The existing city can remain the visual environment and backdrop.

However, the existing city was not originally prepared as a player-driven road network. The autonomous bus follows a spline through decorative environment geometry. The surface beneath it is not necessarily a clean, continuous road designed for vehicle physics.

## The current problem

The original vehicle is an autonomous coach rather than a Zimbabwean commuter kombi. Its movement was controlled by a Bezier spline, so it drove itself instead of responding to the player.

We removed the autonomous movement and created temporary player controls, but this exposed several underlying problems:

- The bus is not the correct type of vehicle.
- The model was not prepared for player-controlled driving.
- It has no proper WheelCollider setup.
- The road surface has inconsistent or unsuitable collision.
- The original vehicle collider did not match the visible model.
- Force-based movement became unreliable against the existing environment geometry.
- The city roads may mainly be decorative pavement rather than proper drivable surfaces.

The current direct-movement controller is therefore a temporary development workaround. It is not the final vehicle system.

## Our revised direction

We will keep the city but prepare one reliable route through it.

Instead of trying to make every street functional, we will create a dedicated road surface along one selected route. This road will be positioned inside the existing city and will provide reliable physics, lane placement and gameplay navigation.

The architecture will be:

```text
Existing city
    → Buildings, sidewalks, scenery and visual background

New road mesh
    → Visible, continuous and collidable player-driving surface

Route spline
    → Helps define the road shape and route direction

Lane waypoints
    → Guide traffic vehicles

Stop markers
    → Define passenger pickup and drop-off locations
```

This allows us to use the city we already have while avoiding its unreliable pavement and collision geometry.

## Planned road work

The first playable route should include:

- One continuous loop or point-to-point route
- Approximately 1–2 km of gameplay road
- Two driving lanes where possible
- Two or three intersections
- Three or four passenger stops
- At least one traffic-light-controlled intersection
- Several potholes or damaged road areas
- A speed bump or roadworks section
- Barriers around unfinished or inaccessible streets
- Road markings and directional signs

The new road mesh will sit slightly above the old pavement to prevent overlapping surfaces and collision problems.

Only the selected route needs full gameplay quality. The rest of the city can remain scenery.

## Vehicle replacement

We plan to replace the current autonomous coach with a more appropriate commuter minibus.

Suitable vehicles include:

- Toyota Hiace-style minibuses
- Nissan Caravan or Urvan-style minibuses
- Other recognizable commuter vans
- Generic models inspired by vehicles commonly used as kombis

Because this is a school assignment and not a commercial release, we can consider free educational and non-commercial assets. We should still respect the licence attached to each model.

Potential free sources include:

- Unity Asset Store free assets
- Sketchfab CC0 models
- Sketchfab CC BY models
- Sketchfab CC BY-NC models
- OpenGameArt
- Kenney
- Other clearly licensed educational assets

We should avoid ripped models from commercial games or models uploaded without download and reuse permission. When attribution is required, we will list the creator, model and licence in the project documentation.

## Vehicle requirements

The selected vehicle should preferably have:

- Correct real-world scale
- Separate wheel meshes
- A usable interior
- Reasonable polygon count
- A clean body mesh
- A clear forward direction
- Space for driver and passenger cameras
- Doors that can eventually be animated
- A licence permitting educational use

If the wheels are not separate, we may need to edit the model in Blender before importing it into Unity.

## Final vehicle physics

We do not need to buy a paid vehicle-physics package. Unity already includes its free WheelCollider vehicle system. The repository also contains an initial WheelCollider-based controller that can be improved and connected to the new vehicle.

The final vehicle will have:

- A Rigidbody
- A simplified body collider
- Four WheelColliders
- Separate visual wheels
- Steering on the front axle
- Powered wheels
- Service braking
- Handbrake support
- Forward and reverse movement
- Speed-sensitive steering
- Suspension
- Centre-of-mass tuning
- Stable camera anchors
- Vehicle reset functionality

The visible vehicle model will be separated from the physics rig. This will allow us to change vehicles without rewriting the driving system.

A proposed structure is:

```text
PlayerVehicle
├── Rigidbody and body collider
├── Vehicle controller
├── WheelCollider_FL
├── WheelCollider_FR
├── WheelCollider_RL
├── WheelCollider_RR
├── Camera anchors
├── Audio sources
└── VisualModel
    ├── Vehicle body
    ├── Front-left wheel
    ├── Front-right wheel
    ├── Rear-left wheel
    └── Rear-right wheel
```

## Replaceable vehicle system

We want the vehicle system to support more than one kombi. Each vehicle can have its own configuration containing:

- Vehicle name
- Mass
- Engine power
- Maximum speed
- Steering angle
- Braking strength
- Suspension settings
- Wheelbase
- Passenger capacity
- Fuel consumption
- Camera positions
- Audio profile

This will eventually support a vehicle-selection screen without requiring separate driving code for every vehicle. For the first milestone, however, we only need one properly working kombi.

## Traffic system

Traffic does not need advanced artificial intelligence. We can create a lightweight waypoint-based system in which traffic vehicles:

- Follow predefined lane points
- Maintain a basic following distance
- Stop behind another vehicle
- Stop at red traffic lights
- Continue on green
- Slow down near intersections
- Respawn or loop when leaving the route
- Use a small selection of vehicle models

For the first playable version, two or three different traffic-car models are enough. These can be reused with different colours. Traffic only needs to exist along the playable route.

## Traffic lights

Traffic lights will use a shared intersection controller. Each controlled intersection can cycle through:

```text
Direction A green
→ Direction A yellow
→ All red
→ Direction B green
→ Direction B yellow
→ All red
```

Traffic vehicles will detect stop lines and obey the active signal. The player may initially receive a penalty for crossing a red light rather than being physically prevented from doing so.

## Passengers and pedestrians

Passengers are central to the game, but they do not initially need complex artificial intelligence. The first passenger system can use simple states:

```text
Waiting
→ Boarding
→ Riding
→ Alighting
→ Completed
```

Passengers can initially stand at a stop, disappear into the vehicle after a boarding animation or delay, and reappear at their destination.

Later improvements may include:

- Walking toward the kombi
- Door animations
- Visible seated passengers
- Different passenger appearances
- Patience timers
- Individual destinations
- Fare negotiation
- Hwindi assistance

General pedestrians can be placed near passenger stops, intersections, shops, pavements and busy roadside areas. They can use simple looping walk paths; we do not need city-wide navigation for the assignment.

## Passenger stops

The first route should contain three or four stops. Each stop will define:

- Stop name
- Pickup area
- Vehicle stopping zone
- Waiting passenger positions
- Available destinations
- Boarding duration
- Route order
- Fare value

The player will need to pull into the stop area, slow down and interact to board or unload passengers.

## Potholes and road hazards

Potholes are an important part of the game's identity and can be implemented as reusable prefabs. Driving through a pothole may:

- Shake the camera
- Play an impact sound
- Reduce vehicle condition
- Affect steering briefly
- Reduce passenger satisfaction
- Increase repair costs

Other hazards can include speed bumps, roadworks, gravel, mud, construction barriers, narrow alternating lanes and broken road surfaces. The project already contains initial scripts for road-surface zones and alternating roadworks, so these can be developed further.

## Game systems

The main shift will track:

- Fare income
- Shift time
- Fuel
- Vehicle condition
- Number of passengers delivered
- Missed passengers
- Traffic violations
- Damage and repair costs
- Route progress

A completed shift will show a summary containing:

- Total fares
- Fuel expenses
- Vehicle damage
- Penalties
- Passengers delivered
- Final take-home amount
- Overall score or rating

## Main menu and interface

The main menu has been redesigned around original Kombi Shift artwork showing a commuter minibus in a Harare-inspired environment.

The interface currently provides:

- Start Shift
- Settings
- Volume control
- Driver profile area
- Route preview
- Keyboard and controller hints

Future menu additions may include vehicle selection, route selection, difficulty options, credits, asset attribution, progress and earnings.

The gameplay HUD should eventually include:

- Speed
- Current route
- Next stop
- Fare earnings
- Shift time
- Fuel
- Vehicle condition
- Passenger information
- Traffic-light or violation notifications

## Visual adaptation of the city

The city does not need complete reconstruction. It needs targeted adaptation, including:

- Replacing obvious New York signs
- Adding Zimbabwean road signs
- Adding kombi-rank signs
- Adding local-style advertisements
- Adjusting road markings
- Adding local vehicle colours and liveries
- Adding suitable vegetation
- Changing lighting and colour grading
- Adding roadside vendors or market props
- Renaming locations
- Adding local ambient audio

Only areas visible from the playable route need detailed changes.

## Scope control

The assignment should focus on a polished five-to-ten-minute experience rather than a large but unfinished city.

The first complete version should contain:

- One main menu
- One proper kombi
- One prepared road route
- Three or four stops
- A small traffic system
- One working traffic-light intersection
- Several potholes
- Basic passengers
- A fare and shift timer
- A shift-completion screen

Once this vertical slice works, additional routes, vehicles, passengers and hazards become extensions rather than fundamental engineering work.

## Proposed development phases

### Phase 1: Vehicle and road foundation

- Find and select a suitable free kombi model.
- Verify its licence.
- Import and clean the model.
- Build the reusable vehicle physics rig.
- Create the dedicated road surface.
- Test collision and handling along the full route.
- Remove the temporary direct-movement controller.

### Phase 2: Route gameplay

- Add stop locations.
- Add route markers.
- Add pickup and drop-off interaction.
- Add fares and shift timing.
- Add fuel and vehicle-condition systems.
- Add the shift-results screen.

### Phase 3: Traffic and hazards

- Add traffic waypoints.
- Add two or three traffic vehicles.
- Add traffic lights.
- Add potholes and speed bumps.
- Add roadworks.
- Add traffic and damage penalties.

### Phase 4: Population and presentation

- Add passenger models.
- Add simple pedestrian loops.
- Add vehicle and city audio.
- Add camera polish.
- Add local signs and advertisements.
- Improve lighting and atmosphere.
- Add attribution and credits.

### Phase 5: Testing and submission

- Test keyboard and controller input.
- Test all route collisions.
- Test passenger stops.
- Test traffic-light behaviour.
- Test shift completion.
- Fix performance issues.
- Create standalone builds.
- Prepare project documentation and presentation material.

## Suggested team responsibilities

### Environment and road

- Build the route surface.
- Repair route collision.
- Add markings, signs, stops and barriers.
- Add local environmental details.

### Vehicle and physics

- Source and prepare the kombi model.
- Separate and align wheels.
- Configure WheelColliders.
- Tune steering, suspension, acceleration and braking.
- Configure vehicle cameras.

### Gameplay systems

- Implement passenger states.
- Implement stops and destinations.
- Implement fares, fuel, damage and shift timing.
- Implement shift completion and results.

### Traffic and pedestrian systems

- Create lane waypoints.
- Implement traffic following and stopping.
- Implement traffic lights.
- Add passengers and pedestrian routes.

### Interface and presentation

- Maintain the menu and HUD.
- Create vehicle and route selection.
- Add audio and visual feedback.
- Maintain credits, documentation and attribution.

Team members can overlap where necessary, but each system should have one person responsible for its integration.

## Current repository state

The current development branch contains:

- The redesigned main menu
- Original menu artwork
- Generated startup and driving scenes
- A temporary player-driving controller
- Player camera controls
- Build configuration
- A standalone launch script
- Explicit rules preventing compiled builds from being committed

The implementation is currently on:

```text
Branch: feat/kombishift2-unity6
Base implementation commit: 9e382e9
```

Compiled standalone binaries are intentionally excluded from Git because they are large and can be regenerated from Unity.

## Final objective

The final project should feel like a small but complete Zimbabwean kombi-driving game—not merely a vehicle moving around a generic city.

The experience should communicate:

- The pressure of completing a shift
- The challenge of city traffic
- The importance of collecting and delivering passengers
- The character of kombi transport
- The effect of potholes and road conditions
- The balance between speed, safety and earnings

Our strategy is to reuse the existing city, introduce one proper road and vehicle system, and build a focused gameplay loop around it. This keeps the workload realistic while still producing a convincing and memorable school project.
