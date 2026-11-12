using System;
using System.Collections.Generic;
using KombiShift2.Gameplay;
using UnityEngine;

namespace KombiShift2.City
{
    // A departure snapshot. Fares use the advertised itinerary through all intermediate stops,
    // not a new shortest-path search for each person or the driver's detours.
    public sealed class CityTripPlan
    {
        readonly float[,,] fares;
        readonly int[] stopIndices;
        readonly IReadOnlyList<int> path;
        public IReadOnlyList<int> Path => path;
        public int StopCount => stopIndices.Length;
        public float Distance { get; }
        public int PathIndexForStop(int index) => stopIndices[index];

        public CityTripPlan(CityRoadNetwork graph, CityTripDefinition trip, List<int> nodes, int[] indices)
        {
            path = nodes.AsReadOnly(); stopIndices = (int[])indices.Clone();
            var distances = new float[nodes.Count];
            for (int i=1;i<nodes.Count;i++) distances[i]=distances[i-1]+Vector3.Distance(graph.Nodes[nodes[i-1]].position,graph.Nodes[nodes[i]].position);
            Distance = distances.Length > 0 ? distances[distances.Length-1] : 0f;
            fares = new float[2,indices.Length,indices.Length];
            for(int origin=0;origin<indices.Length;origin++)
                for(int destination=origin+1;destination<indices.Length;destination++)
                    for(int stage=0;stage<2;stage++)
                        fares[stage,origin,destination]=trip.Fare(distances[indices[destination]]-distances[indices[origin]],(KombiStage)stage);
        }
        public float Fare(int origin,int destination,KombiStage stage)
        {
            if(origin<0 || destination<=origin || destination>=StopCount) throw new ArgumentOutOfRangeException(nameof(destination),"Trip stops must be valid and downstream.");
            return fares[stage==KombiStage.RushHour?1:0,origin,destination];
        }
        // Estimate all configured waiting passengers completing their default destinations.
        // Competition, capacity, skipped pickups and missed stops can change actual earnings.
        public float ExpectedGross(KombiShiftConfiguration settings)
        {
            float gross=0;int boarded=0;
            for(int origin=0;origin<StopCount-1;origin++)
                for(int person=0;person<settings.CityQueueSize;person++)
                    gross+=Fare(origin,++boarded%2==0?StopCount-1:origin+1,settings.Stage);
            return gross;
        }
    }
}
