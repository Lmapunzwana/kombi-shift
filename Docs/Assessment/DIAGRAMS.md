# Current system diagrams — 9 November expansion

```mermaid
flowchart LR
 Stage[StageSettings] --> Director
 Director --> Station[Exclusive station reservation]
 Station --> Character[Physical boarding callback]
 Character --> Trip[PassengerTrip manifest]
 Trip --> Request[Stop request bell and HUD]
 Request --> Alight[Stopped service / alighting animation]
 Alight --> Fare[Completed fare and XP]
 Fare --> Director
 Rigidbody --> Condition[Collision condition adapter]
 Condition --> RCC[RCC input modifiers]
 Rigidbody --> Quality[Service quality sampler]
 Zone[Road rule zones] --> Law[Grace / incidents / fines]
 Law --> Director
 Law --> Quality
 Quality --> HUD
 Rival[Utility evaluator / driving profile] --> Wheel[WheelCollider steering and torque]
 Wheel --> Station
 Rival --> HUD
```

```mermaid
stateDiagram-v2
 [*] --> Onboard: real seat arrival / destination assigned
 Onboard --> Requested: within approach radius
 Requested --> Missed: recedes beyond threshold / one penalty
 Requested --> Alighting: stopped at destination
 Missed --> Alighting: stopped at a valid recovery station
 Onboard --> Alighting: terminal service
 Alighting --> Completed: character reaches arrival seat / fare / XP
```

```mermaid
stateDiagram-v2
 [*] --> Cruise
 Cruise --> ChasePassenger: utility winner / cooldown
 ChasePassenger --> ChasePassenger: new winner exceeds hysteresis
 ChasePassenger --> PullIn: station approach
 PullIn --> Boarding: physical arrival and stopped
 Boarding --> Depart: real boarding callbacks finish
 Depart --> ChasePassenger: free seats and eligible queue
 Depart --> Depart: full / physically drive to Mbare
 ChasePassenger --> Cruise: exhausted or reserved targets
```

Character states (Seated, Walking, Onboard, Returning, Alighting, Delivered) remain separate from trip service states. No event alone proves physical movement: integration checks observe the rigidbody and actual passenger transitions.

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
