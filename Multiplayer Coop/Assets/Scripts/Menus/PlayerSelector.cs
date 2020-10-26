using P2P;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Selector for selecting a character in the lobby menu
/// </summary>
public class PlayerSelector : MonoBehaviour, ISyncObj
{
    [SyncData] public byte id { get; set; }
    [SyncData] public int index { get; set; }
    [SyncData] public bool locked { get; set; }

    private Image sprite;
    [SerializeField] private Image lockSelectionSprite = null;
    private TextMeshProUGUI text;
    [NonSerialized] public Transform parent;

    float[] letterLocation = { -40, -15, 10 };

    private Color playerColor;

    public void OnEnable() {
        sprite = GetComponent<Image>();
        text = transform.GetComponentInChildren<TextMeshProUGUI>();
    }

    // Assign an id and set the color and text variables
    public void AssignId(byte id) {
        this.id = id;
        text.text = id.ToString();
        text.transform.localPosition = new Vector2(letterLocation[id], text.transform.localPosition.y);
        playerColor = sprite.color = GameData.PlayerColors[id];
        lockSelectionSprite.color = Color.clear;
    }

    // Set the new move index and send it over the network
    public void Move(int addition) {
        if (!locked) {
            index += addition;
            if (index > 2)
                index = 0;
            else if (index < 0)
                index = 2;

            UpdateState();
            MultiplayerManager.peerManager.SendDataToAllPeers(GetByteData(), PacketType.charSelect, PacketValue.changeUpdate, true);
        }
    }

    // Update the visual state
    private void UpdateState() {
        transform.SetParent(parent.GetChild(index), false);
        lockSelectionSprite.color = playerColor / (locked ? 1f : 2f) - new Color(0, 0, 0, 0.5f);
        sprite.color = locked ? Color.clear : playerColor;
    }

    // Lock the character selection on the current selection
    public void ToggleLockState() {
        locked = !locked;
        UpdateState();
        MultiplayerManager.peerManager.SendDataToAllPeers(GetByteData(), PacketType.charSelect, PacketValue.changeUpdate, true);
    }

    public List<byte> GetByteData() {
        return DataConverter.ConvertObjectToByte(this);
    }

    public void SyncDataToObj(List<object> objs, float timeDiff) {
        DataConverter.ApplyFieldsToInstance(this, objs);

        UpdateState();
    }
}
