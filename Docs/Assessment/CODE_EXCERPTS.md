# Current source walkthrough ? 12 November reliability changes

These references describe current source, not a claim that the new work passed runtime tests. Imported RCC remains the vehicle engine.

## Resolve challenge rules

`Scripts/Gameplay/KombiStageSettings.cs` is the editable authoring asset. `KombiShiftConfiguration.cs` resolves a departure snapshot. `KombiChallenge` preserves its public stage/score API. Direct-scene delivery targets and city-trip targets are intentionally separate settings.

```csharp
var settings = new KombiShiftConfiguration(KombiChallenge.Selected);
float allowance = settings.TripDuration(routeMetres, stopCount);
```

## Trace a passenger

`KombiShiftGameDirector` owns progression, manifest references and public events. `KombiPassengerService` coordinates `PassengerStation.Board` and `StationPassenger.LeaveAt`. Boarding checks the actual RCC parking brake, speed, alignment and distance. Releasing Space cancels unfinished boarding. Each real seat callback creates a trip; only a delivered actor completes it.

`KombiShiftLedger.CreditDelivery` accepts a completed trip once. It stores integer cents and keeps gross fares, fines, repairs and the time bonus separate. `OnFareEarned` fires after accepted credit.

## Explain city fares

`CityRoadNetwork.PlanTrip` builds a directed itinerary through all required stops, including their exact path indices. `CityTripPlan` caches downstream fares for both stages from cumulative itinerary distances. The menu's potential gross uses that same plan and configured queues. It assumes all waiting passengers are delivered; actual competition/capacity can lower earnings. No boarding callback performs A*.

## Explain queue ownership

`PassengerStation.TryReserve` requires demand, exclusive ownership, a close approach and matching heading/lane. It still allocates unique seats and releases its owner in a finally block. Player and rivals share this production service.

## Explain traffic reliability

`CityTrafficAgent.ResetForPool` releases junction claims, clears path/turn/stop/timing state and resets wheel commands. `CityPhysicsQueries` grows reusable buffers and falls back to a complete query at its cap. `CityIntersection` uses spatial buckets for proximity searches and prunes/releases stale reservations.

`CityTrafficFleet` reuses a prepared spawn path. Agents stagger decision times. Fleet spawning and vehicle replans share the graph's per-frame search count/time budget; searches remain synchronous, so the budget is a soft limit between searches.

## Validation boundary

New unit/component work requires Unit Test MCP. It is unavailable in this session; no new unit test pass or coverage is claimed. Existing scene fixtures now apply the same parking-brake input as the player, but must be rerun. See `Docs/Assessment/CODE_REVIEW_IMPROVEMENTS.md`.
