using System.Collections;
using UnityEngine;
namespace KombiShift.JaydenStarter
{
    public enum PassengerState{Waiting,Boarding,OnBoard,Delivered,Expired,RivalTaken}
    public sealed class PassengerAI:MonoBehaviour
    {
        public PassengerState state;public int origin,destination;public float remaining,maximum,routeDistance,boardedAt,baseFare,agreedFare;
        ShiftGame game;
        public void Configure(ShiftGame owner,int from,int to,float distance,float wait)
        {
            StopAllCoroutines();game=owner;origin=from;destination=to;routeDistance=distance;baseFare=1+distance*.018f;maximum=remaining=wait;state=PassengerState.Waiting;StartCoroutine(Patience());
        }
        public void Board(float now,float multiplier){agreedFare=baseFare*multiplier;state=PassengerState.OnBoard;boardedAt=now;maximum=remaining=30+routeDistance/5;}
        IEnumerator Patience(){while(true){if(game!=null&&game.IsPlaying&&(state==PassengerState.Waiting||state==PassengerState.OnBoard)){remaining=Mathf.Max(0,remaining-Time.deltaTime);if(remaining<=0)state=PassengerState.Expired;}yield return null;}}
        void OnDisable(){StopAllCoroutines();}
        public float Tip=>Mathf.Max(0,baseFare*.25f*Mathf.Clamp01(remaining/Mathf.Max(1,maximum)));
    }
}
