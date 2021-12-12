using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class UnitCommandGiver : MonoBehaviour
{
    [SerializeField] private UnitSelectionHandler unitSelectionHandler = null;      //class reference
    [SerializeField] private LayerMask layerMask = new LayerMask();     //struct
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;       //set the reference to scene camera for raycast calculation where in the scene is clicked
        GameOverHandler.ClientOnGameOver += ClientHandleGameOver;
    }

    private void OnDestroy()
    {
        GameOverHandler.ClientOnGameOver -= ClientHandleGameOver;
    }

    private void ClientHandleGameOver(string winnerName)
    {
        enabled = false;
    }

    private void Update()
    {
        if (!Mouse.current.leftButton.wasPressedThisFrame) return;
        Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask)) return;

        if(hit.collider.TryGetComponent<Targetable>(out Targetable target))
        {
            if (!target.hasAuthority) 
            { 
                TryTarget(target);
                return;
            }
        }
        TryMove(hit.point);
    }

    private void TryTarget(Targetable target)
    {
        foreach (Unit selectedUnit in unitSelectionHandler.SelectedUnits)   
        {
            selectedUnit.GetTargeter().CmdSetTarget(target.gameObject);  
        }
    }

    private void TryMove(Vector3 point)
    {
        foreach(Unit selectedUnit in unitSelectionHandler.SelectedUnits)        //reference from unitSelectionHandler since they are both on same gameobject for the units in the 'selectedUnits' list
        {
            selectedUnit.GetUnitMovement().CmdMove(point);      //ref from Unit script of that particular unitmovement script ...& then setting destination.
        }
    }
}
