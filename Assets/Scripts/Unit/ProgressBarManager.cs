using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Units
{
    public class ProgressBarManager : MonoBehaviour
    {
        [SerializeField]
        private ProgressBar progressBarPrefab;
        [SerializeField]
        private Transform parent;
        [SerializeField]
        ClickableArea clickableArea;
        [SerializeField]
        private Color backgroundColor;
        [SerializeField]
        private Color fillColor;
        [SerializeField]
        Vector2 progressBarScaleMedium = new Vector2(0.1f, 2f);
        [SerializeField]
        float progressBarOffsetMedium;
        [SerializeField]
        float positionAdjustmentStrengthMedium = 0.03f;
        [SerializeField]
        Vector2 progressBarScaleBig = new Vector2(0.2f, 2f);
        [SerializeField]
        float progressBarOffsetBig;
        [SerializeField]
        float positionAdjustmentStrengthBig = 0.03f;
        [SerializeField]
        float progressBarOffsetPlayerBase;

        private Dictionary<Unit, ProgressBar> activeProgressBars = new();
        private const float referenceYResolution = 1920f;

        public static ProgressBarManager Instance { get; private set; }

        void Awake()
        {
            Instance = this;
        }


        public ProgressBar CreateProgressBar(Unit unit)
        {
            if (unit.Data.Size == Size.Small)
                return null;

            ProgressBar progressBar = ObjectPool.Instance.GetObject(progressBarPrefab.gameObject).GetComponent<ProgressBar>();
            progressBar.transform.SetParent(parent);
            progressBar.transform.localScale = GetProgressBarScale(unit);
            progressBar.transform.localRotation = Quaternion.identity;
            activeProgressBars.Add(unit, progressBar);
            progressBar.Init(0);
            progressBar.ChangeColors(backgroundColor, fillColor);
            unit.OnHealthChanged += OnHealthChanged;

            return progressBar;
        }
        

        public void RemoveProgressBar(Unit unit)
        {
            if (activeProgressBars.TryGetValue(unit, out ProgressBar progressBar))
            {
                ObjectPool.Instance.ReturnObject(progressBar.gameObject);
                unit.OnHealthChanged -= OnHealthChanged;
                activeProgressBars.Remove(unit);
            }
        }

        private void OnHealthChanged(Unit unit)
        {
            float fillAmount = (float)unit.Health / (float)unit.Data.MaxHealth * 100f;
            activeProgressBars[unit].SetFillAmount(fillAmount);
        }

        private Vector3 GetProgressBarScale(Unit unit)
        {
            if (unit.Data.Size == Size.Medium)
            {
                return new Vector3(progressBarScaleMedium.x, progressBarScaleMedium.y, 1f);
            }
            else if (unit.Data.Size == Size.Big)
            {
                return new Vector3(progressBarScaleBig.x, progressBarScaleBig.y, 1f);
            }
            return Vector3.one;
        }

        private float GetProgressBarOffset(Unit unit)
        {
            Sides side = NetworkManager.Singleton.IsHost ? Sides.Player : Sides.Enemy;
            if (unit is Base && unit.Team == (int)side)
            {
                return progressBarOffsetPlayerBase;
            }
            else if (unit.Data.Size == Size.Medium)
            {
                return progressBarOffsetMedium;
            }
            else if (unit.Data.Size == Size.Big)
            {
                return progressBarOffsetBig;
            }
            return 0f;
        }

        
        private float GetProgressBarPosAdjustmentStrength(Unit unit)
        {
            if (unit.Data.Size == Size.Medium)
            {
                return positionAdjustmentStrengthMedium;
            }
            else if (unit.Data.Size == Size.Big)
            {
                return positionAdjustmentStrengthBig;
            }
            return 0f;
        }

        private Vector3 GetProgressBarPosition(Unit unit)
        {
            Vector3 worldPos = unit.transform.position;
            Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPos);

            Vector2 screenCenter = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Vector2 screenOffsetFromCenter = (Vector2)screenPos - screenCenter;
            screenPos += (Vector3)screenOffsetFromCenter * GetProgressBarPosAdjustmentStrength(unit);

            return screenPos + GetProgressBarOffset(unit) * Screen.height / referenceYResolution * Vector3.up;
        }

        private void UpdatePositions()
        {
            foreach (var kvp in activeProgressBars)
            {
                Unit unit = kvp.Key;
                ProgressBar progressBar = kvp.Value;
                
                progressBar.transform.position = GetProgressBarPosition(unit);
            }
        }

        private void Update()
        {
            UpdatePositions();
        }
    }
}
