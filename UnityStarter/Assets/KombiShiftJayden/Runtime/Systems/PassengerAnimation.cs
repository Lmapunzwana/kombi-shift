using UnityEngine;
namespace KombiShift.JaydenStarter {
 public sealed class PassengerAnimation:MonoBehaviour {
  PassengerAI person;Animator animator;
  void Awake(){person=GetComponent<PassengerAI>();animator=GetComponent<Animator>();}
  void Update(){if(animator==null)return;animator.SetBool("Walking",person!=null&&person.state==PassengerState.Boarding);}
 }
}
