using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class AnimationHelpers
{
    public static void CreateFieldElixirAnimation(GameObject iconPrefab, Transform parent, Vector3 hitPosition, float value)
    {
        GameObject icon = ObjectPool.Instance.GetObject(iconPrefab);
        Vector3 originalScale = icon.transform.localScale;
        icon.transform.SetParent(parent);
        icon.transform.localRotation = Quaternion.identity;
        icon.transform.position = hitPosition;
        icon.transform.localScale = new Vector3(originalScale.x * 0.5f, originalScale.y * 0.15f, originalScale.z);
        var image = icon.GetComponent<Image>();
        image.material = new Material(image.material);
        image.material.color = new Color(image.material.color.r, image.material.color.g, image.material.color.b, 0);
        TextMeshProUGUI costText = icon.GetComponentInChildren<TextMeshProUGUI>();
        costText.text = "-" + value.ToString();

        Sequence sequence = DOTween.Sequence();
        sequence.Append(icon.transform.DOLocalMoveY(icon.transform.localPosition.y + 800f, 1f).SetEase(Ease.OutCirc));
        sequence.Insert(0, icon.transform.DOScale(originalScale, 1.5f).SetEase(Ease.OutElastic));

        sequence.Insert(0, image.material.DOFade(1, 0.4f).SetEase(Ease.OutCubic));
        sequence.Insert(1.4f, image.material.DOFade(0, 0.4f).SetEase(Ease.InCubic));
        sequence.Insert(1.501f, icon.transform.DOScale(Vector3.zero, 0.4f).SetEase(Ease.InCirc));
        sequence.OnComplete(() =>
        {
            icon.transform.localScale = originalScale;
            ObjectPool.Instance.ReturnObject(icon);
        });
    }
}
