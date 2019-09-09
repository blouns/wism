using System;
using BranallyGames.Wism;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GameManager : MonoBehaviour
{
    public const float StandardTime = 0.25f;
    public const float WarTime = 1.0f;
    private const int DefaultRandom = 1990;

    public static readonly string DefaultModPath = @"Assets\Scripts\Core\mod";

    public static IWarStrategy WarStrategy = new DefaultWarStrategy();

    public static System.Random Random = new System.Random(DefaultRandom);
}

