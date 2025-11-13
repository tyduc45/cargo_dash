using UnityEngine;

public enum CargoState
{
    InPool,     // in object pool waiting for an initial speed
    Active,     // In scene , can be interacted with physics
    Carried,    // pickup by player£¬with icon in queue
    Delivered   // object hitted the receiver
}
