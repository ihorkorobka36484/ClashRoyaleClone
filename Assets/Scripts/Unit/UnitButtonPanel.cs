using DG.Tweening;
using Mono.CSharp;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Units {
    public class UnitButtonPanel : NetworkBehaviour
    {

        [SerializeField]
        Unit[] units = new Unit[4];
        [SerializeField]
        Transform parentForCopy;
        [SerializeField]
        ProgressBar progressBar;
        // <summary>
        /// Area where we can place units
        /// </summary>
        [SerializeField]
        ClickableArea unitPlaceArea;
        // <summary>
        /// Whole area of the game field
        /// </summary>
        [SerializeField]
        ClickableArea overallArea;
        // <summary>
        /// Area which player base occupies
        /// This area is used to prevent placing units on the player base
        /// </summary>
        [SerializeField]
        ClickableArea playerBaseArea;
        // <summary>
        /// Whole area of the game UI plane
        /// </summary>
        [SerializeField]
        ClickableArea panelArea;
        [SerializeField]
        Transform iconPrefab;
        [SerializeField]
        float minCopyScale = 0.5f;

        public static UnitButtonPanel Instance;

        private UnityAction<UnitSpawner.SpawnParams> OnDragEndEvent;
        private UnityAction<Vector3, Type> OnEnteredUnitPlaceAreaEvent;
        private UnityAction<Vector3> OnDragEvent;

        private UnitButton[] buttons;
        private UnitButton copyButton;
        private float originalDistance;
        private Vector3 originalScale;
        private Vector3 lastHitPosition;
        private bool enteredUnitPlaceArea = false;
        private bool active = true;


        void Awake()
        {
            Instance = this;

            buttons = GetComponentsInChildren<UnitButton>();
            for (int i = 0; i < buttons.Length; i++)
            {
                if (i < units.Length)
                {
                    UnitButton button = buttons[i];
                    buttons[i].SetValue(units[i]);
                    buttons[i].SetBeginDrag(() => OnBeginDrag(button));
                    buttons[i].SetEndDrag(() => OnEndDrag(button));
                    buttons[i].SetOnDrag(UpdateButtonCopyPosition);
                }
            }
        }

        public void SetOnDragEvents(UnityAction<Vector3, Type> onEnteredUnitPlaceAreaEvent, UnityAction<UnitSpawner.SpawnParams> onDragEndEvent, UnityAction<Vector3> onDragEvent)
        {
            OnEnteredUnitPlaceAreaEvent = onEnteredUnitPlaceAreaEvent;
            OnDragEvent = onDragEvent;
            OnDragEndEvent = onDragEndEvent;
        }

        public void SetActive(bool active)
        {
            this.active = active;
            if (!active)
            {
                for (int i = 0; i < buttons.Length; i++)
                {
                    buttons[i].GetComponentInChildren<Button>().interactable = false;
                    buttons[i].SetCostProgress(0);
                }
            }
        }

        public void CreateFieldElixirAnimationInMousePosition(float value)
        {
            CreateFieldElixirAnimation(value, panelArea.GetMouseHitPosition());
        }


        public void CreateFieldElixirAnimation(float value, Vector3 position)
        {
            AnimationHelpers.CreateFieldElixirAnimation(iconPrefab.gameObject, parentForCopy, position, value);
        }
        

        public void UpdateButtonsStatus(float elixirValue, Sides side)
        {
            Sides playerSide = NetworkManager.Singleton.IsHost ? Sides.Player : Sides.Enemy;
            if (playerSide != side)
                return;

            for (int i = 0; i < buttons.Length; i++)
            {
                float progress = elixirValue / units[i].Data.Cost;
                bool active = IsButtonActive(side);
                buttons[i].GetComponentInChildren<Button>().interactable = active && elixirValue > units[i].Data.Cost;
                if (!active)
                    progress = 0f;
                buttons[i].SetCostProgress(progress);
            }
        }

        private bool IsButtonActive(Sides side)
        {
            return active && (side == Sides.Player || !ElixirManager.Instance.SpawnLock);
        }

        private void OnBeginDrag(UnitButton button)
        {
            if (!button.GetComponentInChildren<Button>().interactable)
            {
                return;
            }

            copyButton = Instantiate(button, parentForCopy);

            copyButton.transform.localScale = button.transform.localScale;
            copyButton.SetValue(button.Unit, true);
            Vector3 hitPosition = overallArea.GetMouseHitPosition();
            originalDistance = unitPlaceArea.GetDistanceToArea(hitPosition);
            originalScale = button.transform.localScale;
            unitPlaceArea.SetVisible(true);
            progressBar.ShowCostHighlight(true, button.Unit.Data.Cost);
        }

        private void OnEndDrag(UnitButton button)
        {
            // if (copyButton == null)
            // {
            //     return;
            // }

            // Destroy(copyButton.gameObject);
            // copyButton = null;
            originalScale = Vector3.zero;
            originalDistance = float.MaxValue;
            unitPlaceArea.SetVisible(false);

            UnitSpawner.SpawnParams spawnParams = new UnitSpawner.SpawnParams(
                overallArea.GetMouseHitPosition(),//lastHitPosition,
                button.Unit.Type,
                true,//enteredUnitPlaceArea,
                false,//true,
                0);
            OnDragEndEvent?.Invoke(spawnParams);

            enteredUnitPlaceArea = false;
            lastHitPosition = Vector3.zero;
            progressBar.ShowCostHighlight(false);
        }

        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
            {
                OnEndDrag(buttons[0]);
            }
        }

        private void UpdateButtonCopyPosition()
        {
            if (copyButton != null)
            {
                Vector3 hitPosition = overallArea.GetMouseHitPosition();
                copyButton.transform.position = panelArea.GetMouseHitPosition();

                // We can place unit on board only if we have entered UnitPlaceArea at least once
                if (unitPlaceArea.IsMouseOver())
                {
                    enteredUnitPlaceArea = true;
                    OnEnteredUnitPlaceAreaEvent?.Invoke(hitPosition, copyButton.Unit.Type);
                }
                else if (!unitPlaceArea.IsMousePositionAboveCenter(hitPosition))
                {
                    enteredUnitPlaceArea = false;
                }
                SetCopyParametersDependingOnDistance(hitPosition);
                lastHitPosition = AdjustHitPosition(hitPosition);
                OnDragEvent?.Invoke(lastHitPosition);
            }
        }

        private Vector3 AdjustHitPosition(Vector3 hitPosition)
        {
            if (playerBaseArea.GetMouseHitPosition() != Vector3.zero)
            {
                hitPosition = playerBaseArea.GetClosestPositionOutside(hitPosition);
            }

            if (!unitPlaceArea.IsMouseOver())
            {
                hitPosition = unitPlaceArea.GetClosestPoint(hitPosition);
            }

            return hitPosition;
        }

        private void SetCopyParametersDependingOnDistance(Vector3 hitPosition)
        {
            if (copyButton != null)
            {
                float alpha = unitPlaceArea.GetDistanceToArea(hitPosition) / originalDistance;
                float t = Mathf.Clamp01(1f - alpha);
                if (unitPlaceArea.IsMousePositionAboveCenter(hitPosition))
                    alpha = 0;
                float scale = Mathf.Lerp(1f, minCopyScale, t);
                copyButton.transform.localScale = originalScale * scale;
                copyButton.SetAlpha(Mathf.Clamp01(alpha));
            }
        }
    }
}
