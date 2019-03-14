using BranallyGames.Wism;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour
{
    public static readonly string DefaultModPath = @"Assets\Scripts\Core\mod";

    public static IWarStrategy WarStrategy = new DefaultWarStrategy();

    public const float StandardTime = 0.25f;
    public const float WarTime = 1.0f;
}

