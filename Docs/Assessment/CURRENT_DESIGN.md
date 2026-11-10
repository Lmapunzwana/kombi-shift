# Kombi Shift 2 — current design and implementation

Updated 9 November 2026, HAI311 Group 6, deadline 13 November 2026. This source description supersedes the 8 November design. The new expansion PDF under output/pdf describes this implementation; the earlier illustrated PDF, presentation, submission copies and historical build evidence describe the prior release. Human participation and permissions are not inferred from software changes.

## Identity, audience and rationale

A Harare-inspired 3D kombi-driving game for casual keyboard/mouse players aged 14+, particularly students familiar with Zimbabwean transport. This audience is a design hypothesis. Short shifts, visible service requests, identifiable rivals and severity-based consequences make driving choices observable. The game is an arcade simulation, not driver training or a statement of statutory Zimbabwean fines.

The existing imported city, Hiace, passenger rigs, RCC integration, scene GUIDs and title/pause/results flow are retained. The expanded playable itinerary uses connected, previously unused bz0/bz1 roads before the original bz2 city route. It spans approximately 1.25 km and seven service locations. Reusing existing collision geometry and road assets avoids a second disconnected prototype. This is a connected bidirectional itinerary, not a general urban lane-graph pathfinder.

## Player loop and progression

Select Morning Shift or Rush Hour on the title screen (F2), start, locate a queue, drive and stop below 2 km/h. Hold Space while passengers walk through the doorway and occupy separate seats. Each real seat arrival creates a PassengerTrip, with origin, downstream destination, fare, happiness, patience, state and request flag. Alternate arrivals choose the next valid service stop or Mbare; this deterministic mix guarantees intermediate and terminal trips without invalid upstream destinations.

Near a destination, passengers request a stop through visible text and a locally generated bell. Stop in its service radius to run the existing alighting character animation. A completed alighting trip pays its configured fare and earns service XP. Intermediate deliveries count toward the shift target; the player must finish at Mbare before time expires. Fares are earned on completed delivery, replacing the earlier boarding-income rule. A success time bonus is round(remaining seconds × 0.08). Best successful net earnings persist by stage.

A missed request is detected after approaching within 90 m and then receding beyond 144 m. Its one-shot penalty reduces that passenger's happiness by a default 20 and driver XP by 10. A missed passenger can alight at any serviced station, including Mbare. Destination requests are evaluated by one half-second manifest sampler, not a separate polling component per passenger. The minimap target prioritises requested destinations.

Morning: 600 seconds, target 1+, $2 per completed trip, 60% traffic, conservative/competitive rivals, 7.5 m/s base rival speed and 22 s player head start plus stagger.
Rush Hour: 420 seconds, target 6+, $3 per completed trip, all traffic, aggressive/competitive rivals, 10.5 m/s base rival speed and 4 s head start plus stagger.

Assets/Resources/MorningShift.asset and RushHour.asset expose duration, target, rival speed/head start, traffic fraction and fare. Parameters are initial balance values, not conclusions from authentic testers. An expanded route increases travel cost while intermediate alighting increases seat reuse; measure completion and missed-stop rates before raising targets.

## Rival intelligence and physics

RivalKombiAI retains Cruise, ChasePassenger, PullIn, Boarding and Depart. An explicit evaluation pass scores passenger demand capped by free seats, actual forward route distance/travel time, nearby congestion and reservation status. Already reserved queues are ineligible. Reassessment cooldown and a required utility advantage prevent oscillation. Exhausted queues trigger a new search; full rivals proceed to Mbare. Queue ownership still uses PassengerStation.TryReserve, and only physical seat arrivals increment rival totals.

Rivals now use dynamic rigidbodies and four runtime WheelColliders with suspension, bounded steering, motor torque and braking torque. Their root position is not teleported or advanced with MovePosition. The cloned visual vehicle remains intact. A low centre of mass, suspension substeps, continuous collision detection, acceleration limits and speed-dependent corner braking constrain the vehicle.

Left-lane offsets follow travel direction for both rivals and existing traffic. Rivals normally stay in their designated lane. Competitive and aggressive drivers may pass a slower vehicle only on a straight, non-restricted section after clearance checks in the opposing lane ahead and behind; they return when the passed vehicle is behind and their own lane is clear. A blocked or oncoming lane prevents a pass. Competitive and aggressive profiles also use higher speeds, faster target reassessment, stronger acceleration and more decisive passenger-queue selection. A forward-only path evaluation prevents choosing an upstream queue by simply reversing through traffic. At itinerary endpoints, vehicles must physically turn. This is not yet a junction-level lane graph with designated turning bays.

Conservative drivers keep longer gaps. Competitive and aggressive drivers close gaps, accelerate faster, reassess passenger demand sooner and travel faster, but all remain in their designated lane rather than overtaking into opposing traffic. Clearances, stop signs, speed/hump zones and traffic signals influence speed. A blocked vehicle reassesses demand and waits for clearance; there is no teleport recovery or alternate-road graph search.

Rivals have distinct property-block body colours without modifying shared material assets. Names, totals, target distance and matching minimap colours identify competitors. Activity events announce target choices, pickups and departures. Not every player offence is mirrored into a rival penalty record.

## Happiness, XP, damage and economy

KombiServiceQuality broadcasts reaction text and XP changes from one vehicle sampler. Longitudinal/lateral acceleration detect harsh braking, acceleration and steering. Collision and traffic-law systems publish discrete penalties. Cooldowns prevent per-frame punishment. Smooth moving service can earn a small reward after 30 seconds; parking does not farm smooth-driving XP. Passenger happiness is bounded 0–100, and the HUD averages currently active trips. Completed records retain their final score. XP is a separate signed shift total.

KombiVehicleCondition uses collision relative velocity plus impulse/mass. Contacts below 2.5 m/s or weak impulse are ignored, damage is severity scaled, capped at 35 per incident and rate limited. Health bottoms at 10. The custom keyboard-to-RCC adapter multiplies requested throttle and braking by bounded condition factors. Lower power reduces acceleration; brake effectiveness remains at least 75%. The system does not modify vendor DLLs, deform meshes or detach wheels. Severe damage currently has HUD/reaction feedback; dedicated smoke, impact VFX and damaged-engine audio remain polish work.

T repairs at any station within its service radius, below 2 km/h, while the shift is active. Repair price is ceil(missing health × 0.15). It requires sufficient earned funds and restores health only after payment. Fines subtract net earnings and remain separately itemised; a negative shift balance is permitted.

## Roads and traffic rules

The editor upgrade creates three additional queue locations on existing connected roads, extends player/minimap/rival/traffic route references, and adds reusable shallow mesh humps. Humps are 4 m long with a 12 cm peak, use the City Asphalt material, and are placed on straight segments away from crossings. Warnings precede them. Crossing too fast causes a rate-limited complaint; severe overspeed can reduce condition. Physical response uses the actual road collider, hump mesh and RCC suspension. Driven hump validation is separately recorded; geometry alone is not proof of acceptable ride quality.

KombiRoadRuleZone supports configurable speed, stop, crossing, roadworks, no-entry, direction, no-overtaking and hump zones. Give Way behavior and signs are disabled; serialized enum value 2 remains reserved for compatibility. Generated visible signs are limited to three speed-limit markers and warnings for physical humps; crossing and no-passing zones remain invisible triggers. Visible signs stand outside the road. Generated white edge and center carriage markings are non-colliding; the yellow route trace exists on the minimap only. The editor collision repair checks the full route corridor against oversized alpha-cutout tree colliders, replacing only nearby foliage collision with a narrow trunk collider and deduplicating trunk capsules. Trigger bookkeeping handles multiple colliders per vehicle. Speed zones enforce the lowest active limit. Continuous overspeed has tolerance and a three-second grace period, then one severity-scaled fine until compliance. Stationary speed below the limit re-arms the next incident. Stop-zone exit checks whether a full stop occurred; no-entry triggers a fine only when a configured restricted zone is entered, direction checks actual velocity, and prohibited passing checks directed position on the right lane. Incident keys and cooldowns suppress repeat charges.

No claim is made for offences that cannot be reliably detected. All monetary values are configurable game-design values. Additional alternate road branches, wider/narrower road construction and complete intersection right-of-way arbitration remain scope gaps.

## UI, controls and architecture

WASD/arrows drive; Space parking brake; C camera; RMB orbit; wheel zoom; Escape pause; R restart; T repair. Existing menu keyboard focus, help, camera sensitivity/distance and master volume remain. The existing speed/minimap/objective layout is extended with secondary condition, speed-limit, XP, happiness, delivered and fine information. Stop requests have a separate alert area. Rival rows show proximity and totals. Results count cumulative completed trips rather than the current cabin count.

Director owns timer, progression, active manifest, completed trips and net earnings. Stations own exclusive queue reservations. StationPassenger owns physical character states. PassengerTrip owns trip state/data. ServiceQuality owns reaction cooldowns/XP. VehicleCondition owns health/repair. TrafficLaw owns offence incidents/records. RoadRuleZone associates rules with physical areas. RivalDrivingProfile holds exposed utility/driving parameters. StageSettings is reusable stage configuration. Fare, pause, shift-finished, trip-changed, rival-activity, quality and damage events connect feedback to rules.

## Animation, assets and attribution

Imported humanoid rigs continue to use the existing procedural seated/walk/alight pose blending. This is not a newly authored Animator clip graph. RCC supplies player vehicle physics; custom code supplies integration, conditions and service rules. The request/complaint sound is a locally generated tone, with text reactions; it is not recorded speech. Existing assets and licence notices are retained. No paid dependency or external speech service was added. OpenAI Codex assisted implementation and verification at Jayden's request; members must record and understand actual work. Source redistribution permissions, including the RCC notice, still require resolution.

## Evidence and honest limitations

See Docs/Validation/Expansion and EXPANSION_ENGINEERING_REPORT.md for executed test/build results. Historical 8 November evidence remains separately dated. Rule fixtures, staged integration, headless physics, exported builds, native execution and human driving are different evidence scopes. The first integration failures are retained rather than erased. An assertion can be corrected when the game deliberately changes its economic contract; unrelated failures must be investigated.

At least three authentic human sessions, severity analysis, three observation-linked fixes and same-task retests remain required. No tester, feedback, group contribution, assigned-book consultation or panel exercise is fabricated. Native Windows/weak-machine/browser usability and accessibility still need genuine human hardware evidence. Existing PDF/deck/submission snapshots must be regenerated from this updated source before hand-in.

## Passenger layout repair — 10 November 2026

The player and both rivals carry twelve passengers in three columns and four rows, with 0.38 m lateral spacing and 0.6 m row spacing. Seat anchors stay inside the Hiace body, above its floor. Cabin anchors scale the stylized avatars to 75% to fit the usable interior; alighting restores their original walking size. Each station has two physical six-place benches; arrivals reserve free positions, and overflow arrivals stand on the pavement. Passenger pelvis positions align with the actual anchor, and arrivals snap to the final destination before sitting. All seven service points use the forward left lane. Boarding requires a matching heading and lane; rivals queue without overtaking, and reverse traffic restores its saved travel direction at startup.

Use **Kombi Shift > Repair Passenger Seats And Left Stops** only on the saved driving scene outside Play mode. The action backs up the scene and player prefab under `Review/BeforeSeatingLane-*`, preserves passenger references and road assets, and saves the repaired layout. **Inspect Passenger Seats And Left Stops** audits the authored scene and renders disposable previews without saving staged passengers.
