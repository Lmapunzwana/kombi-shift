# Expansion panel practice

Historical expansion material. Current city architecture is in `CityInfrastructure/DESIGN_AND_ARCHITECTURE.md`; 12 November source changes and pending validation are in `CODE_REVIEW_IMPROVEMENTS.md`.
These are exercises to perform, not completed member achievements.

- Explain why a busy distant station can beat a nearby small queue. Change RivalDrivingProfile.passengerValue or switchAdvantage and observe a real target decision. Explain reservation ownership, physical arrival and cooldown.
- Show WheelCollider steering/motor/brake torque, forward/rear/side pass clearance and gradual lane offset. Explain why the current itinerary is not a general lane graph and how endpoint turns can fail.
- Trace a passenger: station coroutine -> actual seat callback -> PassengerTrip -> request -> alighting animation -> completed fare/XP. Show a missed trip and recovery. Character state differs from trip state.
- Change missedStopPenalty or missedStopXP on the director. Demonstrate the one-shot flag and reaction cooldown. Explain why current happiness is separate from cumulative XP.
- Explain relative collision speed plus impulse/mass filtering, incident damage cap and bounded condition multipliers. Show RCC remains the player controller and vendor code is untouched.
- Change a speed-limit zone or grace period. Explain zone contacts with multiple colliders, one continuous speeding incident and separate fine/XP/happiness effects. Distinguish game fines from real law.
- Change MorningShift/RushHour configuration. Explain shared logic, traffic density, rival risk and higher Rush fares. Do not claim balancing from testers until real results exist.
- Read test evidence: identify initial failures, fixes and retests. Distinguish staged positions from driven navigation, editor rule checks from builds, exports from native execution and automated checks from authentic human sessions.
- Demonstrate an actual before/after gameplay profile on a target machine. Explain any unmeasured GPU or hardware claim as a gap, not a result.

Every member must personally practise and record real reviewed/modified files and explanations in CONTRIBUTIONS.csv. AI assistance and imported assets must be disclosed accurately. Existing PDF/slides describe the earlier release until updated.
