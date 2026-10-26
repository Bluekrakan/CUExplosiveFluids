
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;

// This code definately isn't the greatest since I have basically no C# experience.
// I was relying on my small amount of Java knowledge and other applicable wisdom I have from making GMod addons.

[BepInPlugin("needa.uniquename.splosivefluids", "ExplosiveFluids", "1.0.1")]
public class ExplosiveFluidPlugin : BaseUnityPlugin {
    internal static new ManualLogSource Logger;
    private static List<FluidExplosion> explosionQueue = new List<FluidExplosion>();
    private static AudioSource currentTinnitus;
    
    public static void AddNewExplosionInfo( Vector3 position ) {
        FluidExplosion info = new FluidExplosion( position );
        explosionQueue.Add( info );
    }
    
    public static void ClearQueuedExplosions() {
        explosionQueue.Clear();
        Logger.LogInfo( $"Explosive Fluids: Queued explosions have been cleared!" );
    }
    
    public static int GetExplosionsInQueue() {
        return explosionQueue.Count;
        
    }
    
    // Helps prevent your ears from being blown out irl, we replace any previous tinnitus sounds with the new one.
    public static void ReplaceTinnitus( AudioSource newTinnitus ) {
        if ( currentTinnitus ) {
            currentTinnitus.Stop();
        }
        
        currentTinnitus = newTinnitus;
    }
    
    private void Awake() {
        Logger = base.Logger;
        Logger.LogInfo($"Explosive fluids was loaded.");
        
        var harmony = new Harmony("needa.uniquename.splosivefluids");
        harmony.PatchAll();
    }
    
    private void Start() {
    
    }
    
    private void Update() {
        List<int> redundantExplosion = new List<int>();
        int i = 0;
        
        foreach ( FluidExplosion info in explosionQueue ) {
            bool explode = info.Logic();
            if ( !explode ) {
                redundantExplosion.Add( i );
                
            }
            
            i++;
        }
        
        foreach ( int index in redundantExplosion ) {
            explosionQueue.RemoveAt( index );
        }
    }
}

public class FluidExplosion {
    private Vector3 position;
    private float time;
    
    public float GetTimeLeft() {
        return this.time;
    }
    
    public void UpdateTimeLeft() {
        this.time -= Time.deltaTime;
    }
    
    public bool Logic() {
        this.UpdateTimeLeft();
        
        if ( this.time <= 0 ) {
            WorldGeneration.CreateExplosion( new ExplosionParams {
                position = this.position + Vector3.up
                
            } );
            
            return false;
        }
        
        return true;
    }

    public FluidExplosion( Vector3 position ) {
        this.position = position;
        this.time = 0.8f + (float)( Math.Floor( (float)ExplosiveFluidPlugin.GetExplosionsInQueue() / 4f ) * 0.15); 
        // Prevent your computer from being ripped to shreds by adding a small delay for every several new qeued explosions.
        // It works better than you'd think.
    }
}

[HarmonyPatch( typeof( LiquidAffect ), "FixedUpdate" )]
public static class FixedUpdatePatch {
    private static void Postfix( LiquidAffect __instance ) {
        // Basically forked from the decompiled code and edited slightly
        if ( Mathf.Abs( __instance.transform.position.x ) >= WorldGeneration.world.halfWidth || Mathf.Abs( __instance.transform.position.y ) >= WorldGeneration.world.halfHeight ) {
            return;
        }
        
        ValueTuple<float, float, int> valueTuple = FluidManager.main.WaterInfo( WorldGeneration.world.WorldToBlockPos( __instance.transform.position ) );
        
        if ( ( __instance.GetComponent<Body>() || __instance.GetComponent<Limb>() ) && valueTuple.Item3 > 0 ) {
            Vector2Int vector2Int = WorldGeneration.world.WorldToBlockPos( __instance.transform.position );
            FluidManager.main.fluid[vector2Int.x, vector2Int.y] = 0;
            ExplosiveFluidPlugin.AddNewExplosionInfo( __instance.transform.position );
            Sound.Play( "mine", __instance.transform.position, false, true, null, 1f, 1f, false, false );
        }
    }
}

[HarmonyPatch( typeof( PreRunScript ), "Start" )]
public static class MenuPatch {
    private static void Postfix( PreRunScript __instance ) {
        ExplosiveFluidPlugin.Logger.LogInfo( $"Explosive Fluids: Returning to menu, queued explosions are being cleared..." );
        ExplosiveFluidPlugin.ClearQueuedExplosions();
    }
}

/*
[HarmonyPatch( typeof( Sound ), "Play" )]
public static class PlaySoundPatch {
    public static void Postfix( Sound __instance, AudioSource __result ) {
        string name = __result.clip.GetName();
        
        if ( name == "Sound/tinnitus" ) {
            ExplosiveFluidPlugin.ReplaceTinnitus( __result );
            
        }
    }
}*/