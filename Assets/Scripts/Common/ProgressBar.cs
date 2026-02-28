using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBar : MonoBehaviour
{
    [SerializeField]
    private Transform fillTransform;
    [SerializeField]
    private Transform parentTransform;
    [SerializeField]
    private Transform costHighlight;
    [SerializeField]
    private Image backgroundImage;
    [SerializeField]
    public Color backgroundColor;
    [SerializeField]
    private Image fillImage;
    [SerializeField]
    public Color fillColor;
    [SerializeField]
    private GameObject segmentPrefab;
    [SerializeField]
    private GameObject divisorPrefab;
    [SerializeField]
    [Range(0, 1)]
    private float segmentBrightness1 = 0.4f;
    [Range(0, 1)]
    [SerializeField]
    private float segmentBrightness2 = 0.8f;
    [Range(0, 100)]
    [SerializeField]
    private float fillAmount = 0.0f;

    private float imageWidth;
    private int segmentsCount;
    private List<GameObject> segments = new List<GameObject>();
    private List<GameObject> divisors = new List<GameObject>();
    private List<bool> segmentsState = new List<bool>();
    private Vector3 originalCostHighlightPosition;

    public void Init(int segmentsCount)
    {
        this.segmentsCount = segmentsCount;
        if (segmentsCount == 0)
            costHighlight.gameObject.SetActive(false);
        originalCostHighlightPosition = costHighlight.localPosition;
        imageWidth = fillImage.rectTransform.sizeDelta.x;
        ChangeColors(backgroundColor, fillColor);
        CreateSegments();
        SetFillAmount(fillAmount);
    }

    public void ChangeColors(Color backgroundColor, Color fillColor)
    {
            this.backgroundColor = backgroundColor;
            backgroundImage.color = backgroundColor;
            this.fillColor = fillColor;
            fillImage.color = fillColor;
    }

    public void SetFillAmount(float value)
    {
        value = Mathf.Clamp(value, 0, 100);
        fillAmount = value;
        fillTransform.localPosition = new Vector3(-(imageWidth * (100 - fillAmount)) / 100f, fillTransform.localPosition.y, fillTransform.localPosition.z);

        UpdateSegments();
    }

    public void ShowCostHighlight(bool enabled, int cost = 0)
    {
        if (enabled)
        {
            cost = Mathf.Clamp(cost, 0, segmentsCount);
            costHighlight.localPosition = originalCostHighlightPosition
                                             + new Vector3(cost * imageWidth / segmentsCount, originalCostHighlightPosition.y, originalCostHighlightPosition.z);
        }
        else
        {
            costHighlight.localPosition = originalCostHighlightPosition;
        }
    }

    private void UpdateSegments()
    {
        for (int i = 0; i < segmentsCount; i++)
        {
            float value = (i + 1) * 100f / segmentsCount;
            bool isActive = value <= fillAmount;
            if (!segmentsState[i] && isActive)
                StartSegmentAppearAnimation(segments[i], i);
            else if (segmentsState[i] && !isActive)
                StartSegmentDisappearAnimation(segments[i], i);
            if (i < segmentsCount - 1)
            {
                divisors[i].SetActive(isActive);
            }
        }
    }

    private void StartSegmentAppearAnimation(GameObject segment, int index)
    {
        segmentsState[index] = true;

        float x = imageWidth * (1f / segmentsCount * index - 1f / 2f + 1f / segmentsCount / 2f);
        float segmentWidth = imageWidth / segmentsCount;
        Vector3 targetPosition = new Vector3(x, segment.transform.localPosition.y, segment.transform.localPosition.z);
        segment.transform.localPosition = targetPosition - new Vector3(segmentWidth, 0, 0);
        segment.transform.DOLocalMoveX(x, 0.6f).SetEase(Ease.OutBounce);

        Material segmentMaterial = segment.GetComponent<Image>().material;
        Color color = segmentMaterial.color;
        color.a = 1f;
        segmentMaterial.color = color;
        void UpdateSegmentMaterial(float brightness)
        {
            segmentMaterial.SetFloat("_EmissionStrength", brightness);
        }
        DG.Tweening.Sequence seq = DOTween.Sequence();
        seq.Append(
            DOTween.To(UpdateSegmentMaterial, 0f, segmentBrightness1, 0.2f).SetEase(Ease.OutQuint)
        );
        seq.Append(
            DOTween.To(UpdateSegmentMaterial, segmentBrightness1, 0f, 1f).SetEase(Ease.InQuad)
        );
    }

    private void StartSegmentDisappearAnimation(GameObject segment, int index)
    {
        segmentsState[index] = false;

        Material segmentMaterial = segment.GetComponent<Image>().material;
        void UpdateSegmentMaterial(float brightness)
        {
            segmentMaterial.SetFloat("_EmissionStrength", brightness);
        }
        DG.Tweening.Sequence seq = DOTween.Sequence();
        seq.Append(
            DOTween.To(UpdateSegmentMaterial, segmentBrightness1, segmentBrightness2, 0.3f).SetEase(Ease.InBack)
        );
        seq.Append(
            DOTween.To(UpdateSegmentMaterial, segmentBrightness2, 0f, 0.3f).SetEase(Ease.OutBack)
        );
        seq.Insert(0.3f,
            segmentMaterial.DOFade(0f, 0.3f).SetEase(Ease.OutBack)
        );
    }

    private void CreateSegments()
    {
        // Need to start creating segments from the end, because of Canvas rendering order in Unity.
        for (int i = segmentsCount - 1; i >= 0; i--)
        {
            GameObject segment = Instantiate(segmentPrefab, parentTransform);
            Image image = segment.GetComponent<Image>();
            image.material = new Material(image.material);
            Vector3 scale = segment.transform.localScale;
            Vector3 position = segment.transform.localPosition;
            position.x = imageWidth * (1f / segmentsCount * i - 1f / 2f + 1f / segmentsCount / 2f);
            scale.x = 1f / segmentsCount;
            segment.transform.localScale = scale;
            segment.transform.localPosition = position;
            Material segmentMaterial = segment.GetComponent<Image>().material;
            Color color = segmentMaterial.color;
            color.a = 0f;
            segmentMaterial.color = color;
            segment.SetActive(true);
            segments.Insert(0, segment);
            segmentsState.Insert(0, false);
        }

        for (int i = 0; i < segmentsCount; i++)
        {
            if (i < segmentsCount - 1)
            {
                GameObject divisor = Instantiate(divisorPrefab, parentTransform);
                divisor.transform.localPosition =
                    segments[i].transform.localPosition +
                    new Vector3(0.5f / segmentsCount * imageWidth, 0, 0);
                divisor.SetActive(true);
                divisors.Add(divisor);
            }
        }
    }
}
