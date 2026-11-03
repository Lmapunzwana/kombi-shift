using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace KombiShift.JaydenStarter
{
    // Unity GUI receives logical key events on platforms where legacy physical-key polling can disagree.
    // Each event-held key has a bounded lease, is removed on key-up, and is cleared on focus loss.
    public static class ShiftInput
    {
        public static int GUIKeyEvents;
        static readonly Dictionary<KeyCode,float> heldAt=new Dictionary<KeyCode,float>();
        static readonly Dictionary<KeyCode,int> queuedDown=new Dictionary<KeyCode,int>();
        static readonly Dictionary<KeyCode,int> deliveredDown=new Dictionary<KeyCode,int>();
        static float touchThrottle,touchTurn;static bool touchBrake;static int touchFrame=-10;
        static float virtualThrottle,virtualTurn;static bool virtualBrake;static int virtualFrame=-10;
        public static bool testOverride;public static float testThrottle,testTurn;public static bool testBrake;
        public static float Throttle=>testOverride?testThrottle:Mathf.Clamp((Held(KeyCode.W)||Held(KeyCode.UpArrow)?1:0)-(Held(KeyCode.S)||Held(KeyCode.DownArrow)?1:0)+(VirtualFresh?virtualThrottle:0)+(Time.frameCount-touchFrame<=1?touchThrottle:0),-1,1);
        public static float Turn=>testOverride?testTurn:Mathf.Clamp((Held(KeyCode.D)||Held(KeyCode.RightArrow)?1:0)-(Held(KeyCode.A)||Held(KeyCode.LeftArrow)?1:0)+(VirtualFresh?virtualTurn:0)+(Time.frameCount-touchFrame<=1?touchTurn:0),-1,1);
        public static bool Brake=>testOverride?testBrake:Held(KeyCode.Space)||(VirtualFresh&&virtualBrake)||(Time.frameCount-touchFrame<=1&&touchBrake);
        static bool VirtualFresh=>Time.frameCount-virtualFrame<=1&&MouseHeld;
        static bool MouseHeld {
            get {
#if ENABLE_INPUT_SYSTEM
                return Mouse.current!=null&&Mouse.current.leftButton.isPressed;
#else
                return Input.GetMouseButton(0);
#endif
            }
        }
        public static bool Held(KeyCode key)
        {
            if(RawHeld(key))return true;
            return heldAt.TryGetValue(key,out float last)&&Time.unscaledTime-last<.8f;
        }
        static bool RawHeld(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var k=Keyboard.current;if(k==null)return false;
            switch(key){case KeyCode.W:return k.wKey.isPressed;case KeyCode.S:return k.sKey.isPressed;case KeyCode.A:return k.aKey.isPressed;case KeyCode.D:return k.dKey.isPressed;case KeyCode.UpArrow:return k.upArrowKey.isPressed;case KeyCode.DownArrow:return k.downArrowKey.isPressed;case KeyCode.LeftArrow:return k.leftArrowKey.isPressed;case KeyCode.RightArrow:return k.rightArrowKey.isPressed;case KeyCode.Space:return k.spaceKey.isPressed;}
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(key);
#else
            return false;
#endif
        }
        static bool RawDown(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var k=Keyboard.current;if(k==null)return false;
            switch(key){case KeyCode.H:return k.hKey.wasPressedThisFrame;case KeyCode.E:return k.eKey.wasPressedThisFrame;case KeyCode.R:return k.rKey.wasPressedThisFrame;case KeyCode.Escape:return k.escapeKey.wasPressedThisFrame;case KeyCode.Return:return k.enterKey.wasPressedThisFrame;}
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key);
#else
            return false;
#endif
        }
        public static bool Pressed(KeyCode key)
        {
            if(deliveredDown.TryGetValue(key,out int previous)&&previous==Time.frameCount)return false;
            bool queued=queuedDown.TryGetValue(key,out int frame)&&Time.frameCount-frame<=1;
            if(!RawDown(key)&&!queued)return false;
            queuedDown.Remove(key);deliveredDown[key]=Time.frameCount;return true;
        }
        public static void ObserveGUI(Event e)
        {
            if(e.type==EventType.KeyDown&&e.keyCode!=KeyCode.None){
                bool already=heldAt.TryGetValue(e.keyCode,out float time)&&Time.unscaledTime-time<.8f;
                heldAt[e.keyCode]=Time.unscaledTime;GUIKeyEvents++;
                bool delivered=deliveredDown.TryGetValue(e.keyCode,out int frame)&&frame==Time.frameCount;
                if(!already&&!delivered)queuedDown[e.keyCode]=Time.frameCount;
            }
            if(e.type==EventType.KeyUp)heldAt.Remove(e.keyCode);
            if(e.type==EventType.MouseUp){virtualThrottle=virtualTurn=0;virtualBrake=false;virtualFrame=-10;}
        }
        public static void Virtual(float throttle,float turn,bool brake)
        {
            virtualThrottle=MouseHeld?throttle:0;virtualTurn=MouseHeld?turn:0;virtualBrake=MouseHeld&&brake;virtualFrame=Time.frameCount;
        }
        public static void Touch(float throttle,float turn,bool brake){touchThrottle=throttle;touchTurn=turn;touchBrake=brake;touchFrame=Time.frameCount;}
        public static void Clear(){touchThrottle=touchTurn=0;touchBrake=false;touchFrame=-10;heldAt.Clear();queuedDown.Clear();virtualThrottle=virtualTurn=0;virtualBrake=false;virtualFrame=-10;}
    }
}
