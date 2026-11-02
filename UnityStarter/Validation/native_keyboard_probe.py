"""Linux/X11 regression helper: send keys only to the owned Kombi Shift preview window.
Run the player with --kombi-input-probe --kombi-exit first. This is automated input,
not a human playtest. Requires X11 and libXtst; does not install dependencies.
"""
import ctypes as c
import ctypes.util
import time
import sys
from pathlib import Path
x = c.CDLL(ctypes.util.find_library('X11'))
t = c.CDLL(ctypes.util.find_library('Xtst'))
x.XOpenDisplay.argtypes=[c.c_char_p]; x.XOpenDisplay.restype=c.c_void_p
x.XDefaultRootWindow.argtypes=[c.c_void_p]; x.XDefaultRootWindow.restype=c.c_ulong
x.XQueryTree.argtypes=[c.c_void_p,c.c_ulong,c.POINTER(c.c_ulong),c.POINTER(c.c_ulong),c.POINTER(c.POINTER(c.c_ulong)),c.POINTER(c.c_uint)]
x.XFetchName.argtypes=[c.c_void_p,c.c_ulong,c.POINTER(c.c_char_p)]
x.XFree.argtypes=[c.c_void_p]
x.XSetInputFocus.argtypes=[c.c_void_p,c.c_ulong,c.c_int,c.c_ulong]
x.XMapRaised.argtypes=[c.c_void_p,c.c_ulong]
x.XFlush.argtypes=[c.c_void_p]
x.XStringToKeysym.argtypes=[c.c_char_p];x.XStringToKeysym.restype=c.c_ulong
x.XKeysymToKeycode.argtypes=[c.c_void_p,c.c_ulong];x.XKeysymToKeycode.restype=c.c_uint
x.XCloseDisplay.argtypes=[c.c_void_p]
x.XGetInputFocus.argtypes=[c.c_void_p,c.POINTER(c.c_ulong),c.POINTER(c.c_int)]
t.XTestFakeKeyEvent.argtypes=[c.c_void_p,c.c_uint,c.c_int,c.c_ulong]
d=x.XOpenDisplay(None)
if not d:raise SystemExit('No X11 display available')
def find(window,depth=0):
    if depth>6:return None
    root=c.c_ulong();parent=c.c_ulong();children=c.POINTER(c.c_ulong)();count=c.c_uint()
    if x.XQueryTree(d,window,c.byref(root),c.byref(parent),c.byref(children),c.byref(count)):
        found=None
        for i in range(count.value):
            found=find(children[i],depth+1)
            if found:break
        if children:x.XFree(children)
        if found:return found
    name=c.c_char_p()
    if x.XFetchName(d,window,c.byref(name)) and name.value:
        value=name.value.decode(errors='replace');x.XFree(c.cast(name,c.c_void_p))
        if 'Kombi Shift' in value:return window
    return None
root=Path(__file__).resolve().parents[1]
ready=root.parent/'Builds/input-probe-ready.txt'
window=None
for _ in range(120):
    window=find(x.XDefaultRootWindow(d))
    if window:break
    time.sleep(.25)
if not window:raise SystemExit('Owned Kombi Shift preview window not found')
x.XMapRaised(d,window);x.XSetInputFocus(d,window,2,0);x.XFlush(d);time.sleep(.3)
focused=c.c_ulong();revert=c.c_int();x.XGetInputFocus(d,c.byref(focused),c.byref(revert));print('Owned window:',hex(window),'input focus:',hex(focused.value),flush=True)
if '--focus-only' in sys.argv:
    print('Focused owned Kombi Shift preview window.');x.XCloseDisplay(d);raise SystemExit(0)
for _ in range(120):
    if ready.exists():break
    time.sleep(.25)
else:raise SystemExit('Player input probe did not become ready')
held=set()
def key(name,down):
    code=x.XKeysymToKeycode(d,x.XStringToKeysym(name.encode()))
    result=t.XTestFakeKeyEvent(d,code,int(down),0);x.XFlush(d)
    print(name,down,"keycode",code,"sent",result,flush=True)
    if down:held.add(name)
    else:held.discard(name)
try:
    key('w',True);time.sleep(1.5)
    key('d',True);time.sleep(.35);key('d',False);key('w',False)
    key('space',True);time.sleep(1);key('space',False)
    key('s',True);time.sleep(.75);key('s',False)
    key('Escape',True);time.sleep(.1);key('Escape',False);time.sleep(.6)
    key('Escape',True);time.sleep(.1);key('Escape',False)
    print('Sent W, D, SPACE, S and ESC through native X11 keyboard events to the owned game window.')
finally:
    for name in list(held):key(name,False)
    x.XCloseDisplay(d)
