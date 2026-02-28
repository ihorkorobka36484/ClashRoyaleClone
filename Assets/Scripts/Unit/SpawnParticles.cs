using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.AI;

namespace Units
{
    public class SpawnParticles : MonoBehaviour
    {
        [SerializeField]
        GameObject lightPart;
        [SerializeField]
        float lightStartTime = 0.1f;
        [SerializeField]
        GameObject debrisPart;
        [SerializeField]
        float debrisStartTime = 0.9f;
        [SerializeField]
        GameObject smokePart;
        [SerializeField]
        float smokeStartTime = 0.15f;
        [SerializeField]
        float yUpOffset = 10f;

        public float YUpOffset => yUpOffset;

        public Dictionary<float, GameObject> Parts
        {
            get
            {
                return new Dictionary<float, GameObject>
                {
                    { lightStartTime, lightPart },
                    { debrisStartTime, debrisPart },
                    { smokeStartTime, smokePart }
                };
            }
        }

        private Dictionary<GameObject, List<Size>> sizeParts;

        void Awake()
        {
            sizeParts = new Dictionary<GameObject, List<Size>>
            {
                { lightPart, new List<Size> { Size.Big, Size.Medium } },
                { debrisPart, new List<Size> { Size.Big } },
                { smokePart, new List<Size> { Size.Big } }
            };
        }

        public void StartParticlesAnimation(Vector3 startPosition, Transform parent, Size size, Sequence spawnAnimation, TweenCallback OnSpawnAnimationFinish = null)
        {
            float yPos = transform.localPosition.y;
            transform.SetParent(parent, false);
            transform.localPosition = new Vector3(startPosition.x, 0 + yPos, startPosition.z);

            foreach (var part in Parts)
            {
                if (sizeParts[part.Value].Contains(size))
                {
                    spawnAnimation.InsertCallback(part.Key, () =>
                    {
                        part.Value.SetActive(true);
                        part.Value.GetComponent<ParticleSystem>().Play();
                    });
                }
            }
            spawnAnimation.InsertCallback(1.4f, () =>
                {
                    OnSpawnAnimationFinish?.Invoke();
                });
            spawnAnimation.InsertCallback(4f, () =>
                {
                    ObjectPool.Instance.ReturnObject(this.gameObject);
                });
        }

        public void StartSpawnAnimation(Unit unit, Transform parent, TweenCallback OnSpawnAnimationFinish = null, bool onlyParticles = false)
        {
            Transform unitTransform = unit.transform;
            float baseOffset = unit.gameObject.GetComponent<NavMeshAgent>().baseOffset;
            Vector3 startPosition = unitTransform.localPosition + new Vector3(0, baseOffset, 0);

            Sequence spawnAnimation = DOTween.Sequence();
            StartParticlesAnimation(startPosition, parent, unit.Data.Size, spawnAnimation, OnSpawnAnimationFinish);

            if (onlyParticles)
                return;

            TransformAnimation(unit, unitTransform, startPosition, spawnAnimation);
        }

        private void TransformAnimation(Unit unit, Transform unitTransform, Vector3 startPosition, Sequence spawnAnimation)
        {
            Vector3 originalScale = unitTransform.localScale;
            unitTransform.localPosition = new Vector3(startPosition.x, startPosition.y, startPosition.z);
            Vector3 startScale = new Vector3(originalScale.x, originalScale.y * 1.7f, originalScale.z);
            unitTransform.localScale = startScale;
            unit.SetEmissionStrength(0.68f);



            spawnAnimation.Insert(0f, unitTransform.DOLocalMoveY(0, 0.45f).SetEase(Ease.InQuad));
            spawnAnimation.Insert(0f, unitTransform.DOScale(startScale - new Vector3(0, startScale.y * 0.7f, 0), 0.4f).SetEase(Ease.InQuad));
            spawnAnimation.Insert(0.4f, unitTransform.DOScale(originalScale, 1f).SetEase(Ease.OutBounce));
            spawnAnimation.Insert(0.6f,
                DOTween.To(unit.SetEmissionStrength, 0.68f, 0f, 2f).SetEase(Ease.InQuad)
            );
        }

        Vector3 GetAvailablePositionOnNavMesh(Vector3 position, GameObject unitGameObject, float maxDistance = 5f)
        {
            int areaMask = NavMesh.GetAreaFromName(unitGameObject.tag);
            if (NavMesh.SamplePosition(position, out NavMeshHit hit, maxDistance, areaMask))
            {
                return hit.position;
            }
            return position;
        }

        
        void OnDisable()
        {
            lightPart.SetActive(false);
            debrisPart.SetActive(false);
            smokePart.SetActive(false);
        }
    }
}
