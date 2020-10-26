using P2P;
using System.Collections.Generic;
using UnityEngine;

public class BasePlayer : MonoBehaviour, ISyncObj, IEntity
{
    public enum PlayerAttack : byte
    {
        Primary,
        Secondary,
        Special
    }

    [SyncData] public byte id { get; set; }
    [SyncData] public Vector2 position { get; set; }
    [SyncData] public Vector2 cursorDirection { get; set; }
    [SyncData] public bool leftDirection { get; set; }
    [SyncData] public byte health { get; set; }

    private Vector2 localVelocity;
    private Vector2 lastPos;
    private float currentFixedTime;

    public float acceleration = 0.5f;
    public float deceleration = 0.9f;
    public float gravity = 10f;
    public float jumpForce = 10f;

    public float maxWalkSpeed = 4f;

    public int points = 0;

    private Transform[] raycastList;
    [SerializeField] private Transform raycastParent = null;

    public GameObject[] projectileIds;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Interactable currentInteractable;
    public Transform healthBar;
    public GameObject aimer;
    public Collider2D bottomCollider;
    public Collider2D interacterCollider;

    public void AssignId(byte id) {
        this.id = id;
        projectileIds = new GameObject[ProjectileManager.maxProjectilesPerObject];
        health = 100;
        sprite.color = GameData.PlayerColors[id];
        if (id == MultiplayerManager.LocalId) {
            PhysicsManager.PhysicsEvt += PhysicsHandler;
        }
        else {
            rb.interpolation = RigidbodyInterpolation2D.None;
        }
    }

    public List<byte> GetByteData() {
        return DataConverter.ConvertObjectToByte(this); ;
    }

    public void SyncDataToObj(List<object> objs, float timeDiff) {
        lastPos = position;
        currentFixedTime = 0;
        DataConverter.ApplyFieldsToInstance(this, objs);
        FlipSprite(leftDirection);
    }

    private void PhysicsHandler() {
        MultiplayerManager.peerManager.SendDataToAllPeers(GetByteData(), PacketType.playerMove, PacketValue.changeUpdate);
    }

    private void OnEnable()
    {
        sprite = GetComponentInChildren<SpriteRenderer>();
        rb = GetComponentInChildren<Rigidbody2D>();

        raycastList = new Transform[raycastParent.childCount];
        for (int i = 0; i < raycastParent.childCount; i++) {
            raycastList[i] = raycastParent.GetChild(i);
        }

        GameManager.AddEntity(this);
    }

    private void OnDisable() {
        PhysicsManager.PhysicsEvt -= PhysicsHandler;
        GameManager.RemoveEntity(this);
    }

    public void Update() {
        if (id == MultiplayerManager.LocalId) {
            CheckInput();

            if(localVelocity.x != 0)
                FlipSprite(localVelocity.x < 0);

        }
        aimer.transform.position = (Vector2)transform.position + cursorDirection;
        if (health > 100)
            health = 100;
        healthBar.localScale = new Vector3(health / 100f, healthBar.localScale.y, 1);
    }

    // Flip sprite
    private void FlipSprite(bool left) {
        sprite.flipX = leftDirection = left;
    }

    // Assign points to the player
    public void GivePoints(int addition) {
        points += addition;
        if(id == MultiplayerManager.LocalId)
            GameManager.IngameMenu.UpdatePointText(points);
    }

    private void CheckInput() {
        // Get the cursors location
        cursorDirection = (Camera.main.ScreenToWorldPoint(Input.mousePosition) - transform.position);
        cursorDirection = cursorDirection.normalized;

        // Movement
        if (Input.GetKey(KeyCode.A)) {
            localVelocity.x -= acceleration;
        } else if (Input.GetKey(KeyCode.D)) {
            localVelocity.x += acceleration;
        } else {
            localVelocity.x *= deceleration;
        }

        // Jump
        if (Input.GetKeyDown(KeyCode.Space) && CanJump()) {
            localVelocity.y = jumpForce;
        }

        // Invoke interactable
        if(Input.GetKeyDown(KeyCode.E)) {
            currentInteractable?.InvokeAndSendEvents(id);
        }

        // Primary attack
        if (Input.GetMouseButtonDown(0)) {
            int index = CanDoAttack();
            if (index != -1) {
                DoAttack(PlayerAttack.Primary,
                (Vector2)transform.position + cursorDirection,
                cursorDirection,
                (byte)index);
            }
        }
    }

    private void FixedUpdate() {
        // Interpolate between the given and local position
        if (id != MultiplayerManager.LocalId) {
            currentFixedTime += Time.fixedDeltaTime;
            Vector2 curVelocity = (position - lastPos) / PhysicsManager.PHYSICS_UPDATE_INTERVAL;
            rb.MovePosition(lastPos + curVelocity * currentFixedTime);
        } else {
            MovementBehaviour();
            position = transform.position;
        }
    }

    #region Movement 

    private void MovementBehaviour() {
        // Clamp speed
        localVelocity.x = Mathf.Clamp(localVelocity.x, -maxWalkSpeed, maxWalkSpeed);

        localVelocity.y -= gravity * Time.fixedDeltaTime;

        rb.MovePosition((Vector2)transform.position + localVelocity * Time.fixedDeltaTime);
    }

    private bool CanJump() {
        foreach (Transform raycast in raycastList) {
            RaycastHit2D hit = Physics2D.Raycast(raycast.position, Vector2.down);
            if (hit.distance < 0.1f && hit.distance != 0) {
                return true;
            }
        }
        return false;
    }

    #endregion

    #region Attacks

    // Attack of player can be overriden
    protected virtual void PrimaryAttack(Vector2 pos, Vector2 dir, byte attackId = 0, float timeDiff = 0) {
        projectileIds[attackId] = GameManager.ProjectileM.SpawnProjectile(0, pos, dir, Projectile.Owner.Player, id, attackId, timeDiff).gameObject;
    }

    // Check if room for projectile
    public virtual int CanDoAttack() {
        return GeneralTools.GetFirstNullIndexFromArray(projectileIds);
    }

    // Do an attack based on the given parameters
    public virtual void DoAttack(PlayerAttack attackType, Vector2 pos, Vector2 dir, byte attackId = 0, bool sendAttack = true, float timeDiff = 0) {
        switch (attackType) {
            case PlayerAttack.Primary:
                PrimaryAttack(pos, dir, attackId, timeDiff);
                break;
        }
        if (sendAttack) {
            SendAttack(attackType, pos, dir, attackId);
        }
    }

    // Send out that this players is attacking
    public virtual void SendAttack(PlayerAttack attackType, Vector2 pos, Vector2 dir, byte attackId = 0) {
        List<byte> data = new List<byte> { (byte)attackType };
        data.AddRange(DataConverter.ConvertVector2ToByte(pos));
        data.AddRange(DataConverter.ConvertVector2ToByte(dir));
        data.Add(attackId);
        MultiplayerManager.peerManager.SendDataToAllPeers(data, PacketType.playerAttack, PacketValue.changeUpdate, true);
    }

    // Recieve an attack and unpack the attack info
    public virtual void RecieveAttack(List<byte> data, float timeDiff) {
        PlayerAttack type = (PlayerAttack)data[0];
        int index = 1;
        Vector2 pos = DataConverter.ConvertByteToVector2(ref data, ref index); // Get pos
        Vector2 dir = DataConverter.ConvertByteToVector2(ref data, ref index); // Get dir
        byte attackId = data[data.Count - 1]; // Get attack id
        DoAttack(type, pos, dir, attackId, false, timeDiff);
    }
    #endregion

    #region Collisions
    private void OnTriggerEnter2D(Collider2D collision) {
        // Change the id of the hit entity of the projectile
        if (collision.CompareTag("Projectile")) {
            GameManager.ProjectileM.GetProjectile(collision.GetInstanceID()).idHit = id;
        }
        // Check if player in interactable
        if (id == MultiplayerManager.LocalId) {
            if (collision.CompareTag("Interactable")) {
                currentInteractable = GameManager.InteractableM.GetInteractable(collision.GetInstanceID());
            }
        }
    }

    // Player exits interactable
    private void OnTriggerExit2D(Collider2D collision) {
        if (id == MultiplayerManager.LocalId) {
            if (collision.CompareTag("Interactable")) {
                Interactable interactable = GameManager.InteractableM.GetInteractable(collision.GetInstanceID());
                if(interactable == currentInteractable)
                    currentInteractable = null;
            }
        }
    }

    // Collision with a ceiling results in a force pushing the player down (not perfect)
    private void OnCollisionEnter2D(Collision2D collision) {
        if (MultiplayerManager.LocalId == id) {
            if (collision.relativeVelocity.y < 0) {
                if (collision.collider.transform.position.y - collision.collider.bounds.extents.y + 0.5f >=
                collision.otherCollider.transform.position.y + collision.otherCollider.bounds.extents.y + collision.otherCollider.offset.y) {
                    localVelocity.y = collision.relativeVelocity.y / 10;
                }
            }
        }
    }

    // Check if the gravity has to be negated by collision with the floor
    private void OnCollisionStay2D(Collision2D collision) {
        if (MultiplayerManager.LocalId == id) {
            if (bottomCollider == collision.otherCollider) {
                for (int i = 0; i < collision.contactCount; i++) {
                    ContactPoint2D contact = collision.GetContact(i);
                    if (contact.point.y >=
                   collision.otherCollider.transform.position.y - collision.otherCollider.bounds.extents.y + collision.otherCollider.offset.y) {
                        localVelocity.y = 0;
                    }
                }
            }
        }
    }
    #endregion
    
}
