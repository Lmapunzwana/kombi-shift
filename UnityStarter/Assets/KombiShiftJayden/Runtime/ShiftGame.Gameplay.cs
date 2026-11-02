using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace KombiShift.JaydenStarter
{
    public sealed partial class ShiftGame
    {
        UIManager uiManager;AudioManager audioManager;
        GameManager gameManager;FuelSystem fuelSystem;SpawnManager spawnManager;RoadGraph roadGraph;LevelDefinition level;
        Transform cityEnvironment,highwayEnvironment;Material highwayRoad;RivalAI rival;
        readonly List<PassengerAI> waiting=new List<PassengerAI>(),manifest=new List<PassengerAI>();
        readonly List<PassengerStop> stopZones=new List<PassengerStop>();
        readonly List<float> respawnAt=new List<float>();
        readonly List<GameObject> hazards=new List<GameObject>();
        const int Capacity=6;float wallet=8,tipsTotal,noCrashBonus;int crashCount,passengerSerial;Vector3 fuelPreviousPosition;
        public Vector3 PlayerPosition=>driver.transform.position;public Vector3 PlayerForward=>driver.transform.forward;
        public Vector3 RoadPoint(Vector3 p){float x=Rainy?0:(p.x<20?0:40);float closest=p.z;float d=float.MaxValue;foreach(float z in new[]{25f,85,145,205})if(Mathf.Abs(p.z-z)<d){d=Mathf.Abs(p.z-z);closest=z;}return !Rainy&&d<9?new Vector3(Mathf.Clamp(p.x,0,40),.225f,closest):new Vector3(Mathf.Clamp(p.x,x-4.5f,x+4.5f),.225f,Mathf.Clamp(p.z,-20,Rainy?530:250));}
        float FareTarget=>level==null?18:level.target;
        bool AtTarget=>stopIndex<stopZones.Count&&stopZones[stopIndex].Contains(driver.transform.position);
        bool AtFuel=>Vector3.Distance(new Vector3(driver.transform.position.x,0,driver.transform.position.z),level.station)<5.5f;
        public PassengerAI NearestWaiting(Vector3 from)
        {
            PassengerAI best=null;float d=float.MaxValue;foreach(var p in waiting)if(p!=null&&p.state==PassengerState.Waiting){float n=Mathf.Abs(from.x-stops[p.origin].x)+Mathf.Abs(from.z-stops[p.origin].z);if(n<d){d=n;best=p;}}return best;
        }
        public Vector3 PassengerLocation(PassengerAI p)=>stops[p.origin];
        public bool TakeRivalPassenger(PassengerAI p)
        {
            if(p==null||p.state!=PassengerState.Waiting||!waiting.Remove(p))return false;p.state=PassengerState.RivalTaken;spawnManager.Release("passengers",p.gameObject);Notify("Rival collected a waiting passenger at "+stopNames[p.origin]+".");return true;
        }
        void InitializeSystems()
        {
            uiManager=gameObject.AddComponent<UIManager>();uiManager.Load();hudTextScale=uiManager.TextScale;touchControls=uiManager.TouchControls;if(!PlayerPrefs.HasKey("Kombi.Touch"))touchControls=Application.isMobilePlatform||Input.touchSupported;audioManager=gameObject.AddComponent<AudioManager>();audioManager.Configure(music,hwindiVoice);gameManager=gameObject.AddComponent<GameManager>();fuelSystem=gameObject.AddComponent<FuelSystem>();spawnManager=gameObject.AddComponent<SpawnManager>();
            rider.gameObject.AddComponent<PassengerAI>();spawnManager.Register("passengers",rider.gameObject,24);
            var template=Group("Pothole template",world,Vector3.zero).gameObject;Shape("Broken asphalt",PrimitiveType.Cylinder,new Vector3(0,.035f,0),new Vector3(1.35f,.012f,1.9f),Mat(C("272927")),template.transform,false);var zone=template.AddComponent<BoxCollider>();zone.isTrigger=true;zone.center=new Vector3(0,.3f,0);zone.size=new Vector3(1.5f,.8f,2);template.AddComponent<Pothole>();spawnManager.Register("potholes",template,12);
            for(int i=0;i<traffic.Count;i++)spawnManager.RegisterExisting("traffic"+i,traffic[i].transform.gameObject);
            BuildHighway();
            rival=traffic[0].transform.gameObject.AddComponent<RivalAI>();
            ConfigureLevel();
        }
        void ConfigureLevel()
        {
            level=Rainy?LevelDefinition.Highway():LevelDefinition.City();stops=level.stops;stopNames=level.names;shiftSeconds=level.seconds;roadGraph=new RoadGraph(Rainy);
            cityEnvironment.gameObject.SetActive(!Rainy);highwayEnvironment.gameObject.SetActive(Rainy);
            foreach(var s in stopZones)Destroy(s.gameObject);stopZones.Clear();
            for(int i=0;i<stops.Length;i++){var g=Group("Service trigger "+stopNames[i],world,stops[i]);var s=g.gameObject.AddComponent<PassengerStop>();s.Setup(i);stopZones.Add(s);}
            gameManager.StartShift(level);fuelSystem.Configure(level,gameManager);Fuel=fuelSystem.Amount;wallet=Rainy?5:8;fuelPreviousPosition=driver.transform.position;
            spawnManager.ReturnAll("passengers");waiting.Clear();manifest.Clear();respawnAt.Clear();passengerSerial=0;
            for(int i=0;i<stops.Length;i++){respawnAt.Add(0);SpawnAtStop(i,2);}
            spawnManager.ReturnAll("potholes");hazards.Clear();int count=Rainy?12:6;
            for(int i=0;i<count;i++){var h=spawnManager.Acquire("potholes");h.transform.position=new Vector3(i%2==0?-2:2,0,Rainy?80+i*36:45+i*31);h.GetComponent<Pothole>().Configure(this,Rainy?13:7);hazards.Add(h);}
            for(int i=0;i<traffic.Count;i++){spawnManager.ReturnAll("traffic"+i);if(i<=level.trafficCount)spawnManager.Acquire("traffic"+i);if(traffic[i].sound!=null&&traffic[i].transform.gameObject.activeSelf)traffic[i].sound.Play();}
            rival.Configure(this,roadGraph,level.aggression,Rainy);
        }
        void SpawnAtStop(int index,int count)
        {
            for(int j=0;j<count;j++){
                var p=spawnManager.Acquire("passengers").GetComponent<PassengerAI>();int destination=(index+1+(passengerSerial++%(stops.Length-1)))%stops.Length;
                foreach(var renderer in p.GetComponentsInChildren<Renderer>())renderer.enabled=true;p.Configure(this,index,destination,roadGraph.Distance(stops[index],stops[destination]),level.waitingPatience+j*12);p.transform.position=stops[index]+new Vector3(-2.4f,.22f,j*1.1f);waiting.Add(p);
            }
            respawnAt[index]=elapsed+22;
        }
        void UpdatePassengersAndFuel()
        {
            Vector3 p=driver.transform.position;float distance=Vector3.Distance(p,fuelPreviousPosition);fuelPreviousPosition=p;if(distance<8)fuelSystem.Consume(distance,Time.deltaTime);
            Fuel=fuelSystem.Amount;fuelCost=fuelSystem.PurchaseCost;
            for(int i=waiting.Count-1;i>=0;i--)if(waiting[i].state==PassengerState.Expired){missed++;spawnManager.Release("passengers",waiting[i].gameObject);waiting.RemoveAt(i);}
            for(int i=0;i<stops.Length;i++)if(elapsed>=respawnAt[i]){bool has=false;foreach(var q in waiting)if(q.origin==i){has=true;break;}if(!has)SpawnAtStop(i,2);else respawnAt[i]=elapsed+5;}
            aboard=manifest.Count>0;
            PassengerAI candidate=null;foreach(var q in waiting)if(q.origin==stopIndex){candidate=q;break;}
            if(candidate!=null){rider=candidate.transform;patience=candidate.remaining;}
            stopMarker.position=Target+Vector3.up*.08f;
        }
        bool HasWaitingAt(int index){foreach(var p in waiting)if(p.origin==index&&p.state==PassengerState.Waiting)return true;return false;}
        int BestStop()
        {
            int best=0;float d=float.MaxValue;
            foreach(var q in manifest){float n=roadGraph.Distance(driver.transform.position,stops[q.destination]);if(n<d){d=n;best=q.destination;}}
            if(manifest.Count>0)return best;
            var p=NearestWaiting(driver.transform.position);return p==null?0:p.origin;
        }
        float EstimatePayment(PassengerAI p)=>p.state==PassengerState.Expired?0:p.agreedFare+p.Tip;
        IEnumerator BoardPassengers()
        {
            var chosen=new List<PassengerAI>();foreach(var p in waiting)if(p.origin==stopIndex&&p.state==PassengerState.Waiting&&chosen.Count+manifest.Count<Capacity)chosen.Add(p);
            if(chosen.Count==0){Notify(manifest.Count>=Capacity?"Seats full. Select a passenger destination on the map.":"No waiting passengers here. Choose another stop.");yield break;}
            boarding=true;hwindiDepartureRequested=false;hwindiCollecting=false;driver.ParkNow();PlayDoor();int completed=0;
            foreach(var p in chosen){
                p.state=PassengerState.Boarding;Vector3 from=p.transform.position;float duration=hwindi?.72f:1.35f;
                for(float t=0;t<duration;t+=Time.deltaTime){boardingProgress=(completed+t/duration)/chosen.Count;p.transform.position=Vector3.Lerp(from,driver.transform.TransformPoint(new Vector3(-1.2f,0,.5f)),t/duration);yield return null;}
                waiting.Remove(p);p.Board(elapsed,fare);manifest.Add(p);foreach(var renderer in p.GetComponentsInChildren<Renderer>())renderer.enabled=false;p.transform.position=driver.transform.position;completed++;
            }
            aboard=manifest.Count>0;boarding=false;hwindiReadyAt=Time.time+(hwindi?.9f:.25f);if(hwindi)PlayReady();stopIndex=BestStop();PositionPassenger();Notify(manifest.Count+" passengers aboard. Destinations and patience are on the HUD.");
        }
        IEnumerator DeliverPassengers()
        {
            var exits=new List<PassengerAI>();foreach(var p in manifest)if(p.destination==stopIndex||p.state==PassengerState.Expired)exits.Add(p);
            if(exits.Count==0){yield return BoardPassengers();yield break;}
            unloading=true;hwindiDepartureRequested=false;hwindiCollecting=true;driver.ParkNow();PlayDoor();PlayCollectionVoice();int completed=0;float total=0;
            foreach(var p in exits){
                departingRider.gameObject.SetActive(true);Vector3 from=driver.transform.TransformPoint(new Vector3(-1.25f,0,.65f)),to=stops[stopIndex]+new Vector3(-2.4f,.22f,2+completed*.7f);
                for(float t=0;t<.72f;t+=Time.deltaTime){boardingProgress=(completed+t/.72f)/exits.Count;departingRider.position=Vector3.Lerp(from,to,t/.72f);yield return null;}
                float payment=EstimatePayment(p);if(payment>0){tipsTotal+=p.Tip;gross+=payment;wallet+=payment;total+=payment;delivered++;FareEarned?.Invoke(payment);gameManager.PublishFare(payment);}else missed++;
                p.state=PassengerState.Delivered;manifest.Remove(p);spawnManager.Release("passengers",p.gameObject);completed++;
            }
            unloading=false;aboard=manifest.Count>0;hwindiReadyAt=Time.time+.9f;PlayFare();stopIndex=BestStop();PositionPassenger();Notify("Delivered "+exits.Count+". Fares and tips $"+total.ToString("F2")+".");
        }
        void ServiceStop()
        {
            if(boarding||unloading)return;if(driver.Speed>1.2f){Notify("Stop completely before service.");return;}
            if(AtFuel){if(fuelSystem.Refill(ref wallet,out float bought,out float cost)){Fuel=fuelSystem.Amount;fuelCost=fuelSystem.PurchaseCost;PlayFare();Notify("Bought "+bought.ToString("F1")+" fuel for $"+cost.ToString("F2")+".");}else Notify("Insufficient cash or tank full. Fuel costs $"+level.fuelPrice.ToString("F2")+" per unit.");return;}
            if(!AtTarget){Notify("Halt inside the selected stop's painted pickup zone.");return;}
            driver.ParkNow();bool exits=false;foreach(var p in manifest)if(p.destination==stopIndex||p.state==PassengerState.Expired)exits=true;
            if(!exits&&(fare>1.6f||Condition<20)){Notify("Passengers refuse the high fare or unsafe vehicle.");return;}
            boardingRoutine=StartCoroutine(exits?DeliverPassengers():BoardPassengers());
        }
    }
}
