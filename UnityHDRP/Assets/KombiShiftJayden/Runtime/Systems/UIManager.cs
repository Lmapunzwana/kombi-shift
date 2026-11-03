using UnityEngine;
namespace KombiShift.JaydenStarter
{
    public sealed class UIManager:MonoBehaviour
    {
        public float TextScale{get;private set;}=1;public bool TouchControls{get;private set;}
        public void Load(){TextScale=PlayerPrefs.GetFloat("Kombi.HUDScale",1);TouchControls=PlayerPrefs.GetInt("Kombi.Touch",Application.isMobilePlatform?1:0)==1;}
        public void Save(float scale,bool touch){TextScale=Mathf.Clamp(scale,.85f,1.1f);TouchControls=touch;PlayerPrefs.SetFloat("Kombi.HUDScale",TextScale);PlayerPrefs.SetInt("Kombi.Touch",touch?1:0);PlayerPrefs.Save();}
    }
}
