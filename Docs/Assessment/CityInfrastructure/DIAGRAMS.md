# City implementation diagrams

## Runtime data flow

```mermaid
flowchart TD
  Imported[Existing NYBlock02 roads and preserved itinerary] --> Audit[Triangle export and physical scene audit]
  Audit --> Bake[Road mask, directed graph and safe-link exclusions]
  Bake --> Network[Resources: CityRoadNetwork]
  Network --> Market[Trip Market and route preview]
  Catalog[Resources: CityTrips and fare configuration] --> Market
  Market --> Session[CityTripSession]
  Session --> Stops[Actual stations, demand, origin and terminal]
  Session --> Director[Existing shift director]
  Network --> Navigation[A* navigation and legal station approaches]
  Network --> Fleet[Pooled physical civilian traffic]
  Network --> Rivals[Competitive WheelCollider rivals]
  Fleet --> Junctions[Signals, occupancy and priority]
  Rivals --> Junctions
  Director --> Trips[Individual passenger trips]
  Trips --> Marker[Requested stopping ring]
  Trips --> Fares[Completed journeys: gross fares]
  Quality[Happiness and XP] --> Director
  Damage[Collision and physical road wear] --> Quality
  Damage --> Modifiers[RCC input and suspension modifiers]
  Fares --> Net[Net earnings after fines and repairs]
```

## Trip and passenger flow

```mermaid
stateDiagram-v2
  [*] --> Market
  Market --> ValidateRoute: Select route
  ValidateRoute --> Market: No reachable itinerary
  ValidateRoute --> Shift: Start valid trip
  Shift --> Boarding: Stopped on left, matching heading, free seats
  Boarding --> Shift: Actual unique seat arrivals
  Boarding --> Shift: Departure cancels collection
  Shift --> Requested: Destination approaches
  Requested --> Alighting: Valid stopped service
  Requested --> Missed: Destination passed
  Missed --> Alighting: Legal return or valid recovery stop
  Alighting --> Shift: Walk through door, reach reserved destination, credit fare
  Shift --> Results: Terminal reached or timer expires
  Results --> Market: Return
  Results --> Shift: Restart selected route
```

## Road authoring and validation gate

```mermaid
flowchart LR
  Source[Original imported meshes] --> Copy[Copied physical road meshes]
  Copy --> Depress[Local triangle subdivision and depressions]
  Depress --> Collider[Matching rendered mesh and MeshCollider]
  Source --> Graph[Derived centreline graph]
  Collider --> Clearance[Road support and body clearance audit]
  Graph --> Clearance
  Clearance -->|Unsafe links| Exclude[Document and exclude]
  Clearance -->|Safe links| Connected[Strongly connected playable area]
  Connected --> Routes[Nine reachable fictional routes]
  Routes --> Scenario[Full-scene gameplay and physics scenarios]
  Scenario --> Builds[Linux and WebGL build verification]
```

## 12 November source ownership

```mermaid
flowchart LR
  Stage[Stage settings assets] --> Config[Resolved departure configuration]
  Config --> Director[Director: progression and events]
  Plan[Cached itinerary and fares] --> Service[Passenger service coordinator]
  Director --> Service
  Service --> Station[Exclusive station queues]
  Station --> Actor[Physical passenger movement]
  Actor --> Delivery[Completed alighting]
  Delivery --> Ledger[Integer-cent ledger]
  Ledger --> Events[Director fare and result events]
  Budget[Shared traffic search budget] --> Fleet[Pool and replans]
  Fleet --> Junction[Spatial junction lookup and reservations]
  Queries[Complete dense physics queries] --> Junction
```

This describes the changed source. Fresh runtime verification is pending; historical passes do not validate the new composition.
