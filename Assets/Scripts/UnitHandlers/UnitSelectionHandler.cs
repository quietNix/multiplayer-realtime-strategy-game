using System;
using System.Collections.Generic;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

public class UnitSelectionHandler : MonoBehaviour
{
    [SerializeField] private LayerMask layerMask = new LayerMask();
    [SerializeField] private RectTransform unitSelectionArea = null;

    private Camera mainCamera;
    private Vector2 startPosition;

    private RTSPlayer player;

    public List<Unit> SelectedUnits  = new List<Unit>();        //creation of new list of unit

    private void Start()
    {
        mainCamera = Camera.main;
        player = NetworkClient.connection.identity.GetComponent<RTSPlayer>();
        Unit.AuthorityOnUnitDespawned += AuthorityHandleUnitDespawned;        //when unit despawned...remove from list of selected units.
        GameOverHandler.ClientOnGameOver += ClientHandleGameOver;
    }

    private void OnDestroy()
    {
        Unit.AuthorityOnUnitDespawned -= AuthorityHandleUnitDespawned;
        GameOverHandler.ClientOnGameOver -= ClientHandleGameOver;
    }

    private void AuthorityHandleUnitDespawned(Unit unit)
    {
        SelectedUnits.Remove(unit);
    }

    private void ClientHandleGameOver(string winnerName)
    {
        enabled = false;
    }

    private void Update()
    {
        if (Mouse.current.rightButton.wasReleasedThisFrame)
        {
            SelectUnits();
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            SelectDraggedUnits();
        }
        else if(Mouse.current.rightButton.isPressed)
        {
            UpdateSelectionArea();
        }
    }

    private void SelectUnits()
    {
        if (unitSelectionArea.sizeDelta.magnitude == 0)   //select and add clicked unit to selectedUnit list.
        {
            Ray ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask)) return;
            if (!hit.collider.TryGetComponent<Unit>(out Unit unit)) return;
            if (!unit.hasAuthority) return;     //if this client dont has the authority of unit nevermind

            if (!Keyboard.current.shiftKey.isPressed)
            {
                foreach (Unit selectedUnit in SelectedUnits)
                {
                    selectedUnit.Deselect();        //Deselect all units in the 'selectedUnits' list
                }

                SelectedUnits.Clear();            //Clear the 'selectedUnits' list

                SelectedUnits.Add(unit);        //Add the only unit which is clicked to the 'selectedUnit' list

                unit.Select();                  //Select that particular unit only
            }

            if (Keyboard.current.shiftKey.isPressed)
            {
                if (SelectedUnits.Contains(unit))   //if the unit is clicked which is already selected
                {
                    SelectedUnits.Remove(unit);         //remove the unit from 'selectedUnits' list
                    unit.Deselect();                    //Deselect that particular unit only
                    print("removeUnit");
                }
                else SelectedUnits.Add(unit);       //else select that also

                foreach (Unit selectedUnit in SelectedUnits)        //select all unit for 'selectedUnits' list
                {
                    selectedUnit.Select();
                }
            }

        }
        else                                                                        //select and add dragged unit to selectedUnit list.
        {
            Vector2 min = unitSelectionArea.anchoredPosition - (unitSelectionArea.sizeDelta / 2);
            Vector2 max = unitSelectionArea.anchoredPosition + (unitSelectionArea.sizeDelta / 2);
            foreach (Unit unit in player.GetPlayerUnits())
            {
                if (SelectedUnits.Contains(unit)) continue;     

                Vector3 screenPosition = mainCamera.WorldToScreenPoint(unit.transform.position);

                if (screenPosition.x > min.x &&
                    screenPosition.y > min.y &&
                    screenPosition.x < max.x &&
                    screenPosition.y < max.y)
                {
                    SelectedUnits.Add(unit);
                    unit.Select();
                }
            }
            unitSelectionArea.gameObject.SetActive(false);
        }

    }


    private void SelectDraggedUnits()
    {
        if (!Keyboard.current.shiftKey.isPressed)   
        {
            foreach (Unit selectedUnit in SelectedUnits)
            {
                selectedUnit.Deselect();        //Deselect all units in the 'selectedUnits' list
            }
            SelectedUnits.Clear();              //Clear the 'selectedUnits' list
        }

        unitSelectionArea.gameObject.SetActive(true);

        startPosition = Mouse.current.position.ReadValue();

        UpdateSelectionArea();      //more effecient
    }

    private void UpdateSelectionArea()
    {
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        float areaWidth = mousePosition.x - startPosition.x;
        float areaHeight = mousePosition.y - startPosition.y;

        unitSelectionArea.sizeDelta = new Vector2(Mathf.Abs(areaWidth), Mathf.Abs(areaHeight));
        unitSelectionArea.anchoredPosition = startPosition + new Vector2(areaWidth / 2, areaHeight / 2);
    }
}
