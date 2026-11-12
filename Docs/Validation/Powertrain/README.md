# Powertrain and audio verification

11 passed; 0 failed. Native Unity 6000.6.2f1 Linux/Vulkan Game-view run with real wheel-collider physics. Traffic/rivals are disabled to isolate transmission and acceleration.

Maximum measured speed: 94.19 km/h. Maximum RPM while passing 55–65 km/h: 3156. Gears observed: 1, 2, 3, 4, 5, 6. Engine-layer maximum gain: 0.28; maximum configured pitch: 1.15. Audio checks inspect the runtime configuration; loudness remains subject to the player's volume/device.

The abrupt 55 km/h cutoff is replaced with gradual throttle reduction over 95–105 km/h. A 110 km/h emergency physics ceiling remains. RCC engine maximumSpeed is a gearing input, now 160 km/h; the separate road governor sets the actual playable limit. Gear RPM targets are recalculated for the lower engine rev range after registration.

## Unity service errors

The earlier editor log showed DNS resolution failure for services.unity.com and later Asset Store OAuth failures. DNS/HTTPS checks now reach Unity's endpoints; Unity Hub was restarted. Neither authentication error recurred in this fresh verification log. This does not establish a permanent fix for future connectivity failures. No game exception was suppressed or filtered.

If the connection errors return, close Editor and Hub, reopen Hub and check sign-in/network access. Unity's guidance: https://discussions.unity.com/t/error-unityconnectwebrequestexception-token-exchange-failed-due-a-failure-with-the-web-request/1697728
