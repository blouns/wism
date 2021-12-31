using UnityEngine;

public class CityContainer : MonoBehaviour
{
    [SerializeField]
    public bool ImportCitesFromTilemap;

    [SerializeField]
    public bool Reset;

    [SerializeField]
    public GameObject CityPrefab;

    [SerializeField]
    public int TotalCities;
}
