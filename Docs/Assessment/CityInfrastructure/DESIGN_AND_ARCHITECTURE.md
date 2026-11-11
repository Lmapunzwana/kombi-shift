# Existing-city driving simulation — implementation design

11 November 2026. This document supplements the existing GDD and supersedes the earlier single-itinerary limitation for trips selected through the Trip Market. The New York environment is a fictional setting for Harare-named routes; no geographic equivalence or current fare accuracy is claimed.

## Player loop

Choose Morning Shift or Rush Hour in the Trip Market, compare reachable routes and estimated gross fares, then start the selected route. Stop on the left for passengers, follow their individual destination requests, drive smoothly, manage damage and fines, and finish at the selected terminal. Fares are credited after passengers physically finish alighting. Net earnings include deductions for simulated fines and repairs; the result screen identifies the time bonus separately from gross fares.

Morning uses 28 pooled civilian vehicles and six waiting passengers per pickup. Rush Hour uses 48 and eight, with stronger rival profiles, a higher delivery target and peak fare adjustments. Timers include road distance and service allowances rather than blindly reusing the old short-route timer. These are authored balancing values, not results of three-person human playtesting.

## Existing-city preservation and road authoring

The imported city prefab and source assets remain intact. The infrastructure editor action identifies the 25 road-material meshes, applies one shared world-mapped asphalt material using the existing Poly Haven worn-asphalt textures, and keeps the original meshes as `CityRoadSurface.sourceMesh` references. Buildings and pavements do not receive that road material.

Physical road copies clip and subdivide local triangles around depressions, then lower their vertices. The same copied mesh is used for rendering and MeshCollider contact. The original flat collider is replaced by that copy rather than left filling the depression. Good, moderate and severe road profiles are deterministic. The final measured pothole count belongs in the current validation report, not a hardcoded design claim.

White road markings are shared meshes projected onto actual road surfaces. Earlier route-only edge/carriage overlays are disabled to avoid duplicate paint. Junctions add stop lines, crossings and direction arrows where road support is present. Existing speed humps and functional road-rule zones remain in the scene. Alpha-cutout tree cards retain their artwork, while collision uses narrow trunks rather than large transparent planes.

## Navigation data and legal approaches

`CityInfrastructureAudit` exports existing road triangles and the preserved authored itinerary. The offline tools rasterise road support at 2 m, thin the road mask and sample connected centrelines. The inspected existing roundabout has clockwise circulation. A scene audit then checks road-only support and a 1.9 m wide body envelope. Unsafe directional links are excluded, and the largest strongly connected component supplies playable traffic and route endpoints. Inaccessible fragments remain documented rather than being described as fully functional roads.

`CityRoadNetwork` stores positions, directed connections, road widths, speed limits, junction/roundabout tags and passing permissions. Left-lane points are offset from the centreline; wide streets support a second same-direction lane. A* searches directed edge states when heading matters, preventing mid-road U-turns and respecting the final approach to a station. A selected itinerary is calculated through all of its intermediate stop nodes, so the preview and actual stop sequence agree.

This is an at-grade road network derived from this city. It is not an arbitrary multi-level road extractor, commercial navigation engine or geographical Harare map.

## Traffic and competitive AI

Civilian vehicles use simple solid Rigidbody chassis and four WheelCollider suspension contacts. Driver profiles change acceleration, braking, following gaps, preferred speed and passing willingness. Decisions run at controlled intervals. Passing requires a wide same-direction lane, straight clearance, safe front/rear gaps and no nearby junction; it does not use opposing traffic. Vehicles brake for obstructions and attempt legal replanning after prolonged blockage. They do not recover by teleporting through visible obstacles.

Junction controllers alternate green, amber and all-red phases, manage occupancy, check exits and yield to approaching traffic from the right. Stop-controlled entries require a stop; yield entries use occupancy/priority checks. The roundabout follows the baked clockwise connections. Driver aggression changes gaps and speed; selected aggressive rivals can perform occasional rolling stops while still checking intersection occupancy.

Rivals retain their Cruise, ChasePassenger, PullIn, Boarding and Depart states. Their utility uses waiting demand, seats, travel cost, congestion and reservations. They share actual queues with the player, drive physically, use distinct body paint without recolouring shared player materials, and publish activity/pickup totals. Selected city trips use the same network for rival station approaches.

## Passengers, economy and vehicle condition

The 12-seat, three-column/four-row layout remains shared by the player and rivals. Onboard avatars fit the interior at 75% scale and regain their original walking scale outside. Alighting walks through the doorway, with staggered departures and the parking brake held during egress. Destination benches reserve unique supported positions; overflow passengers stand on the pavement.

Each player trip stores its origin, destination, fare, happiness, patience, request and completion state. The stopping ring identifies requests and alignment/speed validity. A missed request preserves the journey and applies rate-limited complaints/XP penalties; navigation can calculate a legal return, and another valid service stop can resolve a missed trip.

Route data configures base fare, distance adjustment, peak multiplier and final fare limits. Expected gross is an estimate; actual passenger fares depend on completed journeys over the graph. Happiness and XP remain separate, event-driven systems. Collisions and high-speed pothole contacts cause cumulative condition, wheel and suspension wear. Damage modifies acceleration, governed maximum speed, braking, steering/alignment and suspension through project-owned code. RCC vendor code is not modified. Stopped service repairs spend actual earnings and restore condition.

## Performance strategy and evidence boundaries

Traffic is pooled and sleeps outside the full-physics radius; offscreen pool reuse avoids visible respawns. Decision intervals, bounded raycast buffers, shared materials, a cached Trip Market road map and distance culling reduce repeated work. Original city objects are not deleted or split into a replacement town.

The profiling scenario compares sequential 15-second Editor windows using the same stage pool: frequent decisions/full city visibility versus controlled decisions/distance culling. Traffic positions evolve between windows, so the observed differences are not a perfectly controlled benchmark or guaranteed player frame rate. Current JSON records contain hardware, frame samples, AI decision timings, available physics-marker data and memory. Human device testing and browser compatibility observations must be recorded separately.
