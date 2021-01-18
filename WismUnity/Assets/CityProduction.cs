using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CityProduction : MonoBehaviour
{
    [SerializeField]
    private Button army1Button;
    [SerializeField]
    private Button army2Button;
    [SerializeField]
    private Button army3Button;
    [SerializeField] 
    private Button army4Button;

    [SerializeField] 
    private Button prodButton;
    [SerializeField] 
    private Button locButton;
    [SerializeField] 
    private Button stopButton;
    [SerializeField]
    private Button exitButton;

    public void OnArmy1Click()
    {
        Debug.Log("Army1 Clicked.");
    }

    public void OnArmy2Click()
    {
        Debug.Log("Army2 Clicked.");
    }

    public void OnArmy3Click()
    {
        Debug.Log("Army3 Clicked.");
    }

    public void OnArmy4Click()
    {
        Debug.Log("Army4 Clicked.");
    }

    public void OnProdClick()
    {
        Debug.Log("Prod Clicked.");
    }

    public void OnLocClick()
    {
        Debug.Log("Loc Clicked.");
    }

    public void OnStopClick()
    {
        Debug.Log("Stop Clicked.");
    }

    public void OnExitClick()
    {
        Debug.Log("Exit Clicked.");
    }
}
