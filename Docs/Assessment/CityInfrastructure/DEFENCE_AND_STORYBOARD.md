# Demonstration storyboard and defence practice

1. **Choose the trip.** Open Trip Market, switch stage, inspect a fictional route, show the existing-city map, fare, demand and expected gross. Explain that these are game values and fictional labels.
2. **Start actual gameplay.** Confirm the selected origin, intermediate stops, terminal and stage traffic. Show the left-side queue and distinct rival colours. Explain reservations and unique cabin seats.
3. **Serve a request.** Follow an individual destination request and the stopping ring, align on the left, stop below 2 km/h, and observe passengers walking through the door. Show fare credited after delivery.
4. **Demonstrate consequences.** Compare slow/fast pothole crossing, condition and complaints. Discuss the measured wheel-contact/force evidence; show stopped paid repair, fines and separate gross/net earnings.
5. **Finish and compare.** Complete a terminal outcome, then compare Rush Hour's demand, traffic and timer. Show actual build/test records and distinguish them from pending human evidence.

## Source walkthrough

- `CityRoadNetwork`: explain data provenance, connected-component exclusions, directed-edge A*, no mid-road U-turns and legal station approaches.
- `CityInfrastructureUpgrade`: show retained `sourceMesh`, copied mesh/collider depressions, shared paint/asphalt and unchanged imported city assets.
- `CityTrafficAgent` and `CityIntersection`: explain physical contacts, following profiles, occupancy, stop/yield logic and same-direction passing.
- `CityTripSession` and `KombiTripMarket`: demonstrate a configured destination changing the actual loaded stops, not just its card text.
- `StationPassenger` and the director: explain boarding/alighting coroutines, seat ownership, destination claims and fare events.
- `KombiRoadWear`, condition and input bridge: show how real contact events feed bounded mechanical modifiers without changing RCC vendor code.

## Suggested live modification

Change a route's base fare or expected demand in `Assets/Resources/CityTrips.json`, refresh/import it, reopen the market and show the changed estimate. Explain why the current in-progress trip should not silently change its completed ledger. Alternatively adjust one driver profile's following gap and observe the same road scenario. Practise this as a real team exercise; the script is not evidence that every member can perform it.

## Questions to prepare

Why preserve the original city? Why can extracted road geometry be unsafe despite graph connectivity? How are potholes prevented from being filled by a flat collider? Why does the path search include heading/previous-edge state? How do reservations prevent duplicate collection? What distinguishes gross fares from net earnings? Why are sequential Editor profiling windows not a guaranteed FPS claim? Which human and browser/device tests are still pending?
