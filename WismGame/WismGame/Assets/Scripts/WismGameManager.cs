using UnityEngine;
using Wism.Client.Common;


public class WismGameManager : MonoBehaviour
{
    string heroName = "Lowenbrau";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        this.heroName = WismHeroNamer.GetRandomHeroName();
        Debug.Log("WismApi: " + heroName);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
