using System.Collections.Generic;
using UnityEngine;
namespace KombiShift.JaydenStarter
{
    public sealed class SpawnManager:MonoBehaviour
    {
        sealed class Pool{public GameObject template;public readonly Queue<GameObject> free=new Queue<GameObject>();public readonly HashSet<GameObject> active=new HashSet<GameObject>();}
        readonly Dictionary<string,Pool> pools=new Dictionary<string,Pool>();public bool UsePooling=true;public int Created{get;private set;}
        public void Register(string key,GameObject template,int capacity)
        {
            template.SetActive(false);var p=new Pool{template=template};pools[key]=p;
            for(int i=0;i<capacity;i++){var g=Instantiate(template,transform);g.name=key+" pooled "+i;g.SetActive(false);p.free.Enqueue(g);Created++;}
        }
        public void RegisterExisting(string key,GameObject instance){instance.SetActive(false);var p=new Pool{template=instance};p.free.Enqueue(instance);pools[key]=p;Created++;}
        bool Pooled(string key)=>UsePooling||key.StartsWith("traffic");
        public GameObject Acquire(string key)
        {
            var p=pools[key];GameObject g;if(Pooled(key)&&p.free.Count>0)g=p.free.Dequeue();else{g=Instantiate(p.template,transform);Created++;}p.active.Add(g);g.SetActive(true);return g;
        }
        public void Release(string key,GameObject g){var p=pools[key];if(g==null||!p.active.Remove(g))return;g.SetActive(false);if(Pooled(key))p.free.Enqueue(g);else Destroy(g);}
        public void ReturnAll(string key){var p=pools[key];var list=new List<GameObject>(p.active);foreach(var g in list)Release(key,g);}
    }
}
