# Source reliability and architecture improvements

12 November 2026. Changes follow AGENTS.md and the existing city architecture. Historical passes do not validate this new source.

## Implemented

- Boarding checks the actual RCC parking brake, speed below 2 km/h, service distance and lane/heading. Releasing Space cancels unfinished boarding; alighting retains automatic braking.
- KombiPassengerService coordinates the existing station/actor coroutines. The director retains progression, manifest references and public events. KombiShiftLedger stores integer cents, prevents duplicate delivery credit and separates gross fares, fines, repairs and time bonus.
- CityTripPlan caches both stage fares from cumulative itinerary distance, through every required stop. Exact stop indices support repeated nodes. Fares are frozen for a departure; boarding performs no path search. The market uses the same model for potential gross, assuming every waiting passenger completes their default journey.
- KombiShiftConfiguration resolves the existing stage assets for the director, city session, fleet and market. It exposes city target, traffic pool, queue size and timer overrides. Zero target/pool/queue/cruise values retain defaults. Actual demand is pickup-stop count times queue size; expectedPassengers remains historical bake-tool metadata.
- Pool reset clears paths, stop/passing state, timers, wheel commands and per-activation counters. Despawn and distant simulation suspension release junction reservations. Visible traffic is not despawned/frozen merely for crossing a distance threshold.
- Dense city cast/overlap buffers grow to 256 and then use complete-query fallbacks. Saturation counters expose crowded cases. Relevant hits are not silently discarded.
- Spatial junction lookup, staggered decisions, reuse of prepared spawn paths and a shared per-frame path-search count/time budget reduce repeated work. The budget is soft between synchronous searches; one search can exceed it.
- Passing respects obstacle/speed limits; stop timing resets when a vehicle moves before completing its stop. Invalid graph/fare data fails safely. Station approaches use incoming directed links. Navigation distinguishes an unavailable route from arrival.
- Pause/results use the selected terminal and show the separate time bonus. HUD target queries no longer sort the manifest. Active guides and code walkthroughs match delivery income, city routes and current ownership.

RCC/vendor code, imported city sources, scenes, prefabs, package manifests, road meshes and baked navigation assets were not edited. Twelve-seat layouts and supported benches remain authoritative. No new city or UI framework was introduced.

## Authoring defaults

Edit MorningShift.asset or RushHour.asset in the Unity Inspector outside Play mode, then start a new trip.

| City setting | Morning | Rush |
|---|---:|---:|
| Delivery target | 4 | 6 |
| Civilian pool | 28 | 48 |
| Queue per pickup | 6 | 8 |
| Timer cruise estimate, m/s | 6 | 7 |
| Allowance per stop, seconds | 40 | 40 |
| Extra trip allowance, seconds | 150 | 150 |

Queue sizes are bounded to twelve supported places and checked against the station template. Legacy targets/fares remain separate existing fields. City fare rates remain in CityTrips.json. Potential gross is not guaranteed income: competition, capacity and skipped pickups affect actual delivery.

## Executed verification

Fresh C# compilation passed with zero errors for the runtime assembly under Editor defines, runtime without UNITY_EDITOR defines, and Editor tools against the new runtime reference assembly. Exact warning counts/method are in ../Validation/CodeReview2026/compilation.json. Existing deprecated-API warnings remain.

The check uses Unity-generated response-file references and Roslyn, omits Unity analyzers and writes temporary assemblies outside the project. It is not an Editor import, build, scene run, visual test, coverage run or performance benchmark. No temporary assembly replaces a live Editor assembly.

Unit Test MCP is unavailable. AGENTS.md and .github/instructions/unittestMcp.instructions.md prohibit terminal substitutes for unit/component generation/review/execution. No new unit tests or coverage passes are claimed. Existing full-scene runners now apply real RCC parking input for staged boarding, but have not been rerun.

The existing Unity Editor remains open without a Pipeline connection; UI activation did not complete. No second Editor was launched, Play mode was not forcibly stopped and no scene was overwritten. If imports have not occurred, use Assets > Refresh after stopping Play yourself.

## Required follow-up validation

1. Through Unit Test MCP, cover ledger duplicate credit/cent arithmetic/insufficient funds; cached fares and repeated stop nodes; stage fallbacks/overrides; invalid graph/fare data; incoming one-way approaches; reset state and saturated collision coverage. Preserve existing assertions and real first-party behavior.
2. Rerun existing city Morning/Rush, menu, passenger-service, grounding and pothole scene workflows. Check held-Space/release cancellation, automatic egress braking, selected-terminal text, intermediate deliveries, exact fares, pause/restart, new-trip settings snapshots and all nine itinerary previews.
3. Run ten-minute congestion observations for both stages on an identified build: signal queues, blocked exits, stop/yield, passing/return, pool despawn/wake and clearing an obstruction. Record stalls/recovery; a stopped vehicle alone is not proof of deadlock. Include more than 24 nearby collider hits and verify nearest obstacles/cross traffic remain detected.
4. Profile the same standalone build/workload on target hardware: frame-time distributions, physics/AI cost, memory/GC, search-budget behavior and culling transitions. Complete-query fallbacks deliberately permit rare allocation for correctness. No fresh FPS improvement is claimed.
5. Fresh builds, native Windows/browser input/audio, visual layout, human comfort/feedback/retests and actual contributions remain separate evidence. Historical PDFs/decks/releases and tester worksheets have not been regenerated as proof.

## Recovery

This extracted workspace has no Git history. Scripts, Docs, README and AGENTS were copied to the sibling _Backup_CodeReview_20261112_125945 folder before editing. The changed-file manifest is in ../Validation/CodeReview2026/changed-files.json. Restore selected source with its .meta files while preserving any later user changes.
