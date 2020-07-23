using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MLAgents;

public class PenguinAcademy : Academy
{
    public float FishSpeed { get; private set;}
    public float FeedRadius { get; private set; }

    public override void InitializeAcademy()
    {
        FishSpeed = 0f;
        FeedRadius = 0f;

        // Set up code to be called every time the fish_speed parameter changes 
        // during curriculum learning
        FloatProperties.RegisterCallback("fish_speed", f =>
        {
            FishSpeed = f;
        });

        // Set up code to be called every time the feed_radius parameter changes 
        // during curriculum learning
        FloatProperties.RegisterCallback("feed_radius", f =>
        {
            FeedRadius = f;
        });
    }
}
