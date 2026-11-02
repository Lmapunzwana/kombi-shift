using UnityEngine;
namespace KombiShift.JaydenStarter
{
    public sealed class PassengerStop:MonoBehaviour
    {
        public int index;public bool playerInside;
        public void Setup(int value){index=value;var zone=gameObject.AddComponent<BoxCollider>();zone.isTrigger=true;zone.center=new Vector3(0,1,0);zone.size=new Vector3(5.8f,3,9);}
        public bool Contains(Vector3 position){Vector3 p=position-transform.position;return Mathf.Abs(p.x)<=2.9f&&Mathf.Abs(p.z)<=4.5f&&Mathf.Abs(p.y)<=3;}
        void OnTriggerEnter(Collider other){if(other.GetComponent<KombiDriver>()!=null)playerInside=true;}
        void OnTriggerExit(Collider other){if(other.GetComponent<KombiDriver>()!=null)playerInside=false;}
    }
}
