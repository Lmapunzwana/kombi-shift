"""Prepare licensed engine audio and clearly attributed synthetic call placeholders.
Requires libsndfile and libespeak-ng installed locally. No audio device or microphone used.
The CC0 engine source is downloaded separately; see Resources/Audio/CREDITS.md.
"""
import array, ctypes as c, ctypes.util, math, wave
from pathlib import Path
out=Path(__file__).resolve().parents[1]/'Assets/KombiShiftJayden/Resources/Audio'
out.mkdir(parents=True,exist_ok=True)
def save(path, samples, rate):
    peak=max(abs(x) for x in samples) or 1
    data=array.array('h',(int(max(-1,min(1,x/peak*.65))*32767) for x in samples))
    with wave.open(str(path),'wb') as f:f.setnchannels(1);f.setsampwidth(2);f.setframerate(rate);f.writeframes(data.tobytes())
class Info(c.Structure):_fields_=[('frames',c.c_longlong),('samplerate',c.c_int),('channels',c.c_int),('format',c.c_int),('sections',c.c_int),('seekable',c.c_int)]
lib=c.CDLL(ctypes.util.find_library('sndfile'));lib.sf_open.argtypes=[c.c_char_p,c.c_int,c.POINTER(Info)];lib.sf_open.restype=c.c_void_p
lib.sf_readf_float.argtypes=[c.c_void_p,c.POINTER(c.c_float),c.c_longlong];lib.sf_readf_float.restype=c.c_longlong;lib.sf_close.argtypes=[c.c_void_p]
info=Info();handle=lib.sf_open(b'/tmp/kombi-bus-idle-source.mp3',0x10,c.byref(info))
if not handle:raise RuntimeError('Could not decode public bus-idle source')
buffer=(c.c_float*(info.frames*info.channels))();count=lib.sf_readf_float(handle,buffer,info.frames);lib.sf_close(handle)
rate=22050;start=int(5*info.samplerate);length=4*rate
samples=[]
for i in range(length):
    frame=start+int(i*info.samplerate/rate)
    samples.append(sum(buffer[frame*info.channels+j] for j in range(info.channels))/info.channels)
# A short overlap removes the boundary discontinuity when the idle loop repeats.
fade=int(.12*rate)
for i in range(fade):
    blend=i/fade;samples[i]=samples[-fade+i]*(1-blend)+samples[i]*blend
save(out/'BusIdle.wav',samples,rate)
es=c.CDLL(ctypes.util.find_library('espeak-ng'))
es.espeak_Initialize.argtypes=[c.c_int,c.c_int,c.c_char_p,c.c_int];es.espeak_Initialize.restype=c.c_int
sample_rate=es.espeak_Initialize(1,0,None,0)
if sample_rate<=0:raise RuntimeError('Could not initialize eSpeak NG retrieval')
chunks=[]
@c.CFUNCTYPE(c.c_int,c.POINTER(c.c_short),c.c_int,c.c_void_p)
def callback(wav,count,events):
    if wav and count>0:chunks.append(c.string_at(wav,count*2))
    return 0
es.espeak_SetSynthCallback.argtypes=[type(callback)];es.espeak_SetSynthCallback(callback)
es.espeak_SetVoiceByName.argtypes=[c.c_char_p];es.espeak_SetVoiceByName(b'en-gb')
es.espeak_SetParameter(1,180,0);es.espeak_SetParameter(3,46,0)
es.espeak_Synth.argtypes=[c.c_void_p,c.c_size_t,c.c_uint,c.c_int,c.c_uint,c.c_uint,c.c_void_p,c.c_void_p]
for name,text in [('HwindiCall','City rank! Market Square! One more passenger!'),('HwindiReady',"All aboard. Let's go!"),('HwindiFare','Your fare please. Thank you.')]:
    chunks.clear();raw=text.encode()+b'\0';ptr=c.create_string_buffer(raw)
    result=es.espeak_Synth(ptr,len(raw),0,1,0,1,None,None)
    if result:raise RuntimeError('Speech synthesis failed')
    es.espeak_Synchronize();data=array.array('h');data.frombytes(b''.join(chunks))
    save(out/(name+'.wav'),[x/32768 for x in data],sample_rate)
    print(name,len(data)/sample_rate,'seconds')
es.espeak_Terminate()
print('Engine and call WAV files prepared; no microphone recordings made.')
