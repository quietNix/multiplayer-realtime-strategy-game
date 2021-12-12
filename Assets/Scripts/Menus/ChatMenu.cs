using System;
using Mirror;
using TMPro;
using UnityEngine;

public class ChatMenu : NetworkBehaviour
{
    [SerializeField] private GameObject chatUI = null;
    [SerializeField] private TMP_Text chatText = null;
    [SerializeField] private TMP_InputField inputField = null;
    [SerializeField] private RTSPlayer player = null;

    private static event Action<string> OnMessage;

    static string oldPlayer;

    public override void OnStartClient()
    {
        if (!hasAuthority) return;
        chatUI.SetActive(true);
        OnMessage += HandleNewMessage;

    }

    [ClientCallback]//dont call on the server
    private void OnDestroy()
    {
        if (!hasAuthority) return;
        OnMessage -= HandleNewMessage;
    }



    [Client]
    public void SendMessage(string message)
    {
        if (!Input.GetKeyDown(KeyCode.Return)) return;
        if (string.IsNullOrWhiteSpace(message)) return;
        CmdSendMessage(message);
        inputField.text = string.Empty;
    }

    [Command]
    private void CmdSendMessage(string message)
    {
        //validate sent too many messages
        if (oldPlayer == player.GetDisplayName()) RpcHandleMessage($"{message}");
        else RpcHandleMessage($"\n[{player.GetDisplayName()}]: {message}");
        oldPlayer = player.GetDisplayName();
    }

    [ClientRpc]
    private void RpcHandleMessage(string message)
    {
        OnMessage?.Invoke($"\n{ message}");
    }
    
    private void HandleNewMessage(string message)
    {
        chatText.text += message;
    }
}
