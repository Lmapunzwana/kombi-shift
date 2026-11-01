using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
namespace KombiShift.JaydenStarter
{
    // Direct keys avoid dependency on project-specific Horizontal/Vertical axis mappings.
    public static class ShiftInput
    {
        public static int GUIKeyEvents;
        static readonly HashSet<KeyCode> guiHeld = new HashSet<KeyCode>();
        static float virtualThrottle, virtualTurn; static bool virtualBrake; static int virtualFrame=-10;
        public static bool testOverride; public static float testThrottle, testTurn; public static bool testBrake;
        public static float Throttle => testOverride ? testThrottle : Mathf.Clamp((Held(KeyCode.W)||Held(KeyCode.UpArrow)?1:0)-(Held(KeyCode.S)||Held(KeyCode.DownArrow)?1:0)+(Time.frameCount-virtualFrame<=1?virtualThrottle:0),-1,1);
        public static float Turn => testOverride ? testTurn : Mathf.Clamp((Held(KeyCode.D)||Held(KeyCode.RightArrow)?1:0)-(Held(KeyCode.A)||Held(KeyCode.LeftArrow)?1:0)+(Time.frameCount-virtualFrame<=1?virtualTurn:0),-1,1);
        public static bool Brake => testOverride ? testBrake : Held(KeyCode.Space)||(Time.frameCount-virtualFrame<=1&&virtualBrake);
        public static bool Held(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var k=Keyboard.current;
            if(k!=null) {
                switch(key) {
                    case KeyCode.W:return k.wKey.isPressed; case KeyCode.S:return k.sKey.isPressed;
                    case KeyCode.A:return k.aKey.isPressed; case KeyCode.D:return k.dKey.isPressed;
                    case KeyCode.UpArrow:return k.upArrowKey.isPressed;case KeyCode.DownArrow:return k.downArrowKey.isPressed;
                    case KeyCode.LeftArrow:return k.leftArrowKey.isPressed;case KeyCode.RightArrow:return k.rightArrowKey.isPressed;
                    case KeyCode.Space:return k.spaceKey.isPressed;
                }
            }
#elif ENABLE_LEGACY_INPUT_MANAGER
            if(Input.GetKey(key))return true;
#endif
            return guiHeld.Contains(key);
        }
        public static bool Pressed(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            var k=Keyboard.current;if(k==null)return false;
            switch(key){case KeyCode.E:return k.eKey.wasPressedThisFrame;case KeyCode.R:return k.rKey.wasPressedThisFrame;case KeyCode.Escape:return k.escapeKey.wasPressedThisFrame;case KeyCode.Return:return k.enterKey.wasPressedThisFrame;}
            return false;
#else
            return Input.GetKeyDown(key);
#endif
        }
        public static void ObserveGUI(Event e)
        {
            if(e.type==EventType.KeyDown){guiHeld.Add(e.keyCode);GUIKeyEvents++;}
            if(e.type==EventType.KeyUp)guiHeld.Remove(e.keyCode);
        }
        public static void Virtual(float throttle,float turn,bool brake){virtualThrottle=throttle;virtualTurn=turn;virtualBrake=brake;virtualFrame=Time.frameCount;}
        public static void Clear(){guiHeld.Clear();virtualThrottle=virtualTurn=0;virtualBrake=false;virtualFrame=-10;}
    }
}
