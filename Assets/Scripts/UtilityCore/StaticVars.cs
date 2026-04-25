
using System;
using UnityEngine;

public static class Layers
{
    public static int Default = 0;
    public static int TransparentFX = 1;
    public static int IgnoreRaycast = 2;
    public static int NonSolid = 3;
    public static int Water = 4;
    public static int UI = 5;
    public static int Player = 6;
    public static int Enemy = 7;
    public static int NPC = 8;
    public static int Projectile = 9;
    public static int InvisWall = 10;
}

/// <summary>
/// Provides predefined execution order constants for various game systems and entities.
/// </summary>
/// <remarks>These constants define the relative execution order for different components in the game loop. Lower
/// values indicate earlier execution. Use these constants to ensure consistent and predictable ordering of gameplay
/// systems, player systems, and other game-related behaviors.</remarks>
public static class ExecutionOrders
{
    public const int Gameplay = -101;
    public const int GameplaySystems = -100;

    public const int Player = -91;
    public const int PlayerSystems = -90;
    public const int PlayerBehaviors = -89;

    public const int Area = -71;
    public const int Room = -70;
}