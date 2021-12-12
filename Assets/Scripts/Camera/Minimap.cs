using System;
using Mirror;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Minimap : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [SerializeField] private RectTransform minimapRect = null;
    [SerializeField] private float mapScale = 5f;
    [SerializeField] private float offset = 10f;

    private Transform playerCameraTransform;

    private void Update()
    {
        if (playerCameraTransform) return;
        if (!NetworkClient.connection.identity) return;
        playerCameraTransform = NetworkClient.connection.identity.GetComponent<RTSPlayer>().GetCameraTransform();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        MoveCamera();
    }

    public void OnDrag(PointerEventData eventData)
    {
        MoveCamera();
    }

    private void MoveCamera()
    {
        //if (1 << gameObject.layer != Convert.ToInt32(layerMask)) return;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            minimapRect,
            mousePos,
            null,
            out Vector2 localPoint)) return;

        Vector2 lerp = new Vector2(
            (localPoint.x - minimapRect.rect.x) / minimapRect.rect.width,
            (localPoint.y - minimapRect.rect.y) / minimapRect.rect.height);

        Vector3 newCameraPos = new Vector3(
            Mathf.Lerp(mapScale, -mapScale, lerp.x),
            playerCameraTransform.position.y,
            Mathf.Lerp(mapScale, -mapScale, lerp.y));

        playerCameraTransform.position = newCameraPos + new Vector3(0f, 0f, offset);
    }


}
