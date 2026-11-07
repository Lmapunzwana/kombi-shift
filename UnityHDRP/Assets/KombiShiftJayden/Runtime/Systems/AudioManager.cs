using UnityEngine;
namespace KombiShift.JaydenStarter
{
    public sealed class AudioManager:MonoBehaviour
    {
        AudioSource music,voice;public float Effects{get;private set;}public float Music{get;private set;}
        public void Configure(AudioSource musicSource,AudioSource voiceSource){music=musicSource;voice=voiceSource;AudioListener.pause=false;AudioListener.volume=1;}
        public void SetMix(float musicLevel,float effectsLevel){Music=Mathf.Clamp01(musicLevel);Effects=Mathf.Clamp01(effectsLevel);if(music!=null)music.volume=Music*.4f;if(voice!=null)voice.volume=Effects*.65f;}
        public void PauseVoice(bool paused){if(voice!=null){if(paused)voice.Pause();else voice.UnPause();}}
    }
}
