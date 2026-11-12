using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

// Reflection keeps the existing default runtime assembly intact: no vendor/reference migration.
public sealed class ExpansionRulesTests
{
    static Type Runtime(string name) => Type.GetType(name + ", Assembly-CSharp", true);
    static object Trip() => Activator.CreateInstance(Runtime("KombiShift2.Gameplay.PassengerTrip"));
    static object Call(object target, string name, params object[] args) => target.GetType().GetMethod(name).Invoke(target,args);
    static object Field(object target,string name) => target.GetType().GetField(name).GetValue(target);
    [TestCase(100f,false)] [TestCase(50f,true)] [TestCase(90f,true)]
    public void StopRequestUsesApproachRadius(float distance,bool expected)
    { Assert.That(Call(Trip(),"Request",distance,90f),Is.EqualTo(expected)); }
    [Test] public void RequestAndMissedStopAreOneShot()
    {
        var trip=Trip(); Assert.That(Call(trip,"Request",50f,90f),Is.True);
        Assert.That(Call(trip,"Request",50f,90f),Is.False);
        Assert.That(Call(trip,"CheckMissed",50f,90f),Is.False);
        Assert.That(Call(trip,"CheckMissed",160f,90f),Is.True);
        Assert.That(Call(trip,"CheckMissed",170f,90f),Is.False);
        Assert.That(Field(trip,"happiness"),Is.EqualTo(80f));
    }
    [TestCase(-1000f,0f)] [TestCase(1000f,100f)] [TestCase(-25f,75f)]
    public void HappinessIsBounded(float delta,float expected)
    { var trip=Trip(); Call(trip,"Adjust",delta); Assert.That(Field(trip,"happiness"),Is.EqualTo(expected)); }
    [TestCase(2f,3f,0f)] [TestCase(10f,.1f,0f)] [TestCase(5f,3f,5f)] [TestCase(100f,30f,35f)]
    public void ImpactsUseThresholdAndCap(float speed,float impulse,float expected)
    {
        var method=Runtime("KombiShift2.Vehicle.KombiVehicleCondition").GetMethod("ImpactDamage");
        Assert.That(method.Invoke(null,new object[]{speed,impulse,2.5f,2f}),Is.EqualTo(expected));
    }
    [TestCase(1f,-1f)] [TestCase(-1f,1f)]
    public void LeftLaneFollowsTravelDirection(float direction,float expectedSign)
    {
        var method=Runtime("KombiShift2.Traffic.RivalKombiAI").GetMethod("LeftOffset");
        var offset=(Vector3)method.Invoke(null,new object[]{Vector3.forward*direction,1.55f});
        Assert.That(Mathf.Sign(offset.x),Is.EqualTo(expectedSign));
    }
    [Test] public void UtilityRespondsToDemandSeatsAndReservation()
    {
        var profile=Activator.CreateInstance(Runtime("KombiShift2.Traffic.RivalDrivingProfile"));
        float Utility(int demand,int seats,float distance,bool reserved)=>(float)Call(profile,"Utility",demand,seats,distance,8f,0,reserved);
        Assert.That(Utility(6,6,100,false),Is.GreaterThan(Utility(1,6,5,false)));
        Assert.That(Utility(6,1,10,false),Is.EqualTo(Utility(1,1,10,false)));
        Assert.That(Utility(6,6,10,true),Is.LessThan(Utility(6,6,10,false)));
    }
    [Test] public void PerformanceRetainsControlAtWorstCondition()
    {
        var go=new GameObject("condition test");
        try {
            var condition=go.AddComponent(Runtime("KombiShift2.Vehicle.KombiVehicleCondition")); Call(condition,"ApplyDamage",1000f);
            Assert.That(condition.GetType().GetProperty("Health").GetValue(condition),Is.EqualTo(10f));
            Assert.That((float)condition.GetType().GetProperty("PowerFactor").GetValue(condition),Is.InRange(.45f,1f));
            Assert.That((float)condition.GetType().GetProperty("BrakeFactor").GetValue(condition),Is.InRange(.75f,1f));
        } finally { UnityEngine.Object.DestroyImmediate(go); }
    }
}
