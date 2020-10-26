using P2P;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Keeps track of the interval at which data is send about players and physics
/// </summary>
public class PhysicsManager : MonoBehaviour
{
    public delegate void PhysicsEvent();
    public static event PhysicsEvent PhysicsEvt;
    public static float PHYSICS_UPDATE_INTERVAL = 0.05f;

    private void Awake() {
        StartCoroutine(UpdatePhysics());
    }

    private void OnDisable() {
        StopAllCoroutines();
    }

    // Update the physics events
    public IEnumerator UpdatePhysics() {
        PhysicsEvt?.Invoke();
        yield return new WaitForSeconds(PHYSICS_UPDATE_INTERVAL);
        StartCoroutine(UpdatePhysics());
    }
}
