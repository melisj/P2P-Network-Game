using P2P;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Physics objects are simulated on the host and are send out to the other peers
/// </summary>
public class PhysicsObject : MonoBehaviour, ISyncObj
{
    [SyncData] public byte id { get; set; }
    [SyncData] public Vector2 position { get; set; }
    [SyncData] public Vector2 velocity { get; set; }
    [SyncData] public float rotation { get; set; }
    public Vector2 prevPos;
    public Vector2 prevVelocity;
    public float prevRotation;
    private Rigidbody2D rb;

    private float accuracyValue = 0.01f;
    private float accelerationThreshold = 0.5f;

    public float currentTime;
    private bool isMoving;
    private bool wasMoving;

    public void AssignId(byte id) {
        this.id = id;
    }

    private void OnEnable() {
        rb = GetComponentInChildren<Rigidbody2D>();
        position = transform.position;
        prevPos = position;
    }

    private void FixedUpdate() {
        // Update the position and rotation of an object when it changes position
        if (MultiplayerManager.IsHost) {
            isMoving = position != (Vector2)transform.position || transform.rotation.eulerAngles.z != rotation;

            if (wasMoving || isMoving) {
                position = transform.position;
                velocity = rb.velocity;
                rotation = transform.rotation.eulerAngles.z;

                ObjectManager.movedObjList.Add(this);
            }

            wasMoving = isMoving;
        }
    }

    public void SyncDataToObj(List<object> objs, float timeDiff) {
        prevPos = position;
        prevRotation = rotation;
        currentTime = 0;
        DataConverter.ApplyFieldsToInstance(this, objs);

        Vector2 curVelocity = (position - prevPos) / PhysicsManager.PHYSICS_UPDATE_INTERVAL;
        float acceleration = curVelocity.magnitude - prevVelocity.magnitude;

        // New position based on the time it took to calculate the physics on the host
        Vector2 calculatedPos = position + curVelocity * (timeDiff * 2 + PhysicsManager.PHYSICS_UPDATE_INTERVAL);

       
       

        // Caclulate how accruate the prediction needs to be for it to trigger a authoritive state change
        accuracyValue = curVelocity.magnitude + 0.01f;
        if (acceleration > -0.05f) {
            accuracyValue += Mathf.Clamp(accelerationThreshold - acceleration, 0, accelerationThreshold);
        }

        // Set the state change if the local state deviates too much from the host's state
        if (Vector2.Distance(position, transform.position) > accuracyValue &&
        Vector2.Distance(calculatedPos, transform.position) > accuracyValue
        ) {
            rb.MovePosition(Vector2.MoveTowards(position, transform.position, Time.fixedDeltaTime));
            rb.velocity = velocity;
        }
        // Set rotation for static objects 
        if (Mathf.Abs(prevRotation - rotation) > accuracyValue) {
            transform.rotation = Quaternion.Euler(0, 0, rotation);
        }
        prevVelocity = curVelocity;
    }

    public List<byte> GetByteData() {
        return DataConverter.ConvertObjectToByte(this);
    }
}
