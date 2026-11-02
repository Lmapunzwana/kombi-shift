# Audio sources and preparation

## Recorded bus engine

- **Bus Engine Idling**, bikesnbassboi, 20 October 2020.
- Source and licence: https://freesound.org/people/bikesnbassboi/sounds/540398/
- Source page identifies the recording as a bus engine recorded from below and licenses it **CC0**: https://creativecommons.org/publicdomain/zero/1.0/
- Public HQ preview retrieved 1 November 2026: https://cdn.freesound.org/previews/540/540398_11267296-hq.mp3
- `BusIdle.wav`: 4-second excerpt, converted to mono 22050 Hz, loop-boundary overlap and peak normalization. This is a real recorded engine, not the former sine-tone placeholder. It is not a recording of a specific Zimbabwean kombi model.

## Temporary hwindi speech

`HwindiCall.wav`, `HwindiReady.wav`, `HwindiFare.wav` are synthesized English placeholders from original project text using locally installed **eSpeak NG** (https://github.com/espeak-ng/espeak-ng). These are not human recordings and do not represent a Shona accent or a real conductor. The eSpeak engine/voice data are not included in the project. Replace these WAVs with consented team recordings for a more authentic final performance.

Scripts: “City rank! Market Square! One more passenger!”, “All aboard. Let's go!”, “Your fare please. Thank you.”

`Tools/prepare_audio.py` records the preparation steps. Horn, door slide/latch, brake scrub and coin effects are original procedural effects in `ShiftGame.HwindiAudio.cs`. Music is optional and off by default; engine/effects default to a moderate level. Audio settings affect the game only; system speaker volume was not changed.
