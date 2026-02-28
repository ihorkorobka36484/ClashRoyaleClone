using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Units{
    public class UnitButton : MonoBehaviour
    {
        [SerializeField]
        private UnitButtonReferances unitButtonReferances;
        [SerializeField]
        private EventTrigger eventTrigger;
        [SerializeField]
        private Image image;
        [SerializeField]
        private Button button;
        [SerializeField]
        private TextMeshProUGUI costText;
        [SerializeField]
        private GameObject elixirIcon;

        public Unit Unit => unit;
        public Button Button => button;

        private Unit unit;
        private Material material;
        private bool canAfford = false;
        private Vector3 originalScale;

        void Awake()
        {
            originalScale = transform.localScale;
        }

        public void SetValue(Unit unit, bool isCopy = false)
        {
            this.unit = unit;
            if (unitButtonReferances.Data.TryGetValue(unit, out Texture texture))
            {
                image.material = new Material(image.material);
                material = image.material;
                if (image != null)
                {
                    Sprite sprite = Sprite.Create((Texture2D)texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                    image.sprite = sprite;
                    image.material.mainTexture = sprite.texture;
                }
                costText.text = unit.Data.Cost.ToString();
            }
            else
            {
                Debug.LogError("Unit not found in UnitButtonReferences.");
            }
            elixirIcon.SetActive(!isCopy);
        }

        public void SetAlpha(float alpha)
        {
            Color color = material.color;
            color.a = alpha;
            material.color = color;
        }

        public void SetCostProgress(float value)
        {
            value = Mathf.Clamp01(value);
            material.SetFloat("_Progress", value);
            if (!canAfford && value == 1)
            {
                canAfford = true;
                Sequence seq = DOTween.Sequence();
                seq.Append(transform.DOScale(originalScale * 1.05f, 0.1f).SetEase(Ease.InSine))
                   .Append(transform.DOScale(originalScale, 0.1f).SetEase(Ease.OutSine));
            }
            if (value < 1)
                canAfford = false;
        }
        
        public void SetBeginDrag(Action OnBeginDrag)
        {
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.BeginDrag;
            entry.callback.AddListener((eventData) =>
            {
                OnBeginDrag();
            });
            eventTrigger.triggers.Add(entry);
        }

        public void SetEndDrag(Action OnEndDrag) {
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.EndDrag;
            entry.callback.AddListener( (eventData) => {
                OnEndDrag();
            } );
            eventTrigger.triggers.Add(entry);
        }

        public void SetOnDrag(Action OnDrag) {
            EventTrigger.Entry entry = new EventTrigger.Entry();
            entry.eventID = EventTriggerType.Drag;
            entry.callback.AddListener( (eventData) => { OnDrag(); } );
            eventTrigger.triggers.Add(entry);
        }
    }
}
