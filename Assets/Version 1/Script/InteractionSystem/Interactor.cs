using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UniSense;
using UnityEngine.SceneManagement;

public class Interactor : MonoBehaviour
{
    [SerializeField] private Transform _interactionPoint;
    [SerializeField] private float _interactionPointRadius = 0.5f;
    [SerializeField] private LayerMask _interactableMask;
    [SerializeField] private InteractionPromptUI _interactionPromptUI;
    [SerializeField] private GameObject _puzzleCanvas;

    private readonly Collider[] _colliders = new Collider[3];
    [SerializeField] private int _numFound;

    private Interactable _interactable;

    [SerializeField] private PlayerInput playerInput;

    private void Start()
    {
        playerInput.SwitchCurrentActionMap("Player");
    }

    private void Update()
    {
        _numFound = Physics.OverlapSphereNonAlloc(_interactionPoint.position,
        _interactionPointRadius, _colliders, _interactableMask);

        if (_numFound == 0)
        {
            if (_interactable != null)
                _interactable = null;

            if (_interactionPromptUI.IsDisplayed)
                _interactionPromptUI.Close();

            if (_interactionPromptUI.gameObject.activeSelf)
                _interactionPromptUI.gameObject.SetActive(false);

            return;
        }

        _interactable = _colliders[0].GetComponent<Interactable>();

        if (_interactable == null)
            return;

        if (!_interactionPromptUI.IsDisplayed)
            _interactionPromptUI.SetUp(_interactable.InteractionPrompt);

        if (!_interactionPromptUI.gameObject.activeSelf)
            _interactionPromptUI.gameObject.SetActive(true);

        bool isCirclePressed = Gamepad.current?.buttonEast.wasPressedThisFrame ?? false;

        if (isCirclePressed)
        {
            // ✅ SavePoint → เรียก Interact() ของมันเอง (ไม่ไปยุ่งกับปริศนา)
            if (_interactable is SavePoint)
            {
                _interactable.Interact(this);
            }
            else
            {
                // ── โค้ดปริศนาเดิม ไม่แตะ ──
                if (Inventory.Instance != null && Inventory.Instance.CorrectPuzzle)
                {
                    Debug.Log("Door is Open");
                }
                else
                {
                    Debug.Log("Play Puzzle");
                    if (_puzzleCanvas != null)
                    {
                        _puzzleCanvas.SetActive(true);
                    }
                    else
                    {
                        Debug.LogError("_puzzleCanvas is not assigned in the Inspector!");
                    }
                    if (playerInput != null)
                    {
                        playerInput.SwitchCurrentActionMap("PuzzleUI1");
                    }
                    else
                    {
                        Debug.LogError("playerInput is not assigned in the Inspector!");
                    }
                }
            }
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_interactionPoint.position, _interactionPointRadius);
    }

}