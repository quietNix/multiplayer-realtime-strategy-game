using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using TMPro;
using UnityEngine;

public class ResourcesDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text resourceText = null;
    private RTSPlayer player = null;

    private void Start()
    {
        player = NetworkClient.connection.identity.GetComponent<RTSPlayer>();
        ClientHandleResourcesUpdate(player.GetResources());
        player.ClientOnResourcesUpdated += ClientHandleResourcesUpdate;
    }

    private void OnDestroy()
    {
        player.ClientOnResourcesUpdated -= ClientHandleResourcesUpdate;
    }

    private void ClientHandleResourcesUpdate(int newResources)
    {
        resourceText.text = $"Resources: {newResources}";
    }
}
