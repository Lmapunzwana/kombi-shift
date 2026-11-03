using UnityEngine;
namespace KombiShift.JaydenStarter
{
    public sealed class LevelDefinition
    {
        public string name;public Vector3[] stops;public string[] names;public float seconds,target,startingFuel,fuelPerMetre,fuelPrice,waitingPatience,aggression;public int trafficCount;public Vector3 station;
        public static LevelDefinition City()=>new LevelDefinition{name="CBD MORNING RUSH",stops=new[]{new Vector3(-4.6f,0,25),new Vector3(-4.6f,0,85),new Vector3(-4.6f,0,145),new Vector3(35.4f,0,25),new Vector3(35.4f,0,85),new Vector3(35.4f,0,145)},names=new[]{"Market Square","Central Avenue","Mbare Link","Eastgate","Fourth Street","Copacabana"},seconds=300,target=18,startingFuel=100,fuelPerMetre=.035f,fuelPrice=.2f,waitingPatience=90,aggression=1,trafficCount=9,station=new Vector3(4,0,110)};
        public static LevelDefinition Highway()=>new LevelDefinition{name="RAINY-SEASON HIGHWAY",stops=new[]{new Vector3(-4.6f,0,45),new Vector3(-4.6f,0,185),new Vector3(-4.6f,0,325),new Vector3(-4.6f,0,465)},names=new[]{"Highway Rank","Tollgate","Norton Turn","Terminus"},seconds=300,target=28,startingFuel=45,fuelPerMetre=.08f,fuelPrice=.3f,waitingPatience=100,aggression=1.5f,trafficCount=4,station=new Vector3(4,0,280)};
    }
}
