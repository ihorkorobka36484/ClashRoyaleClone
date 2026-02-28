using UnityEngine;
using DG.Tweening;
using System;

public class ArrowFlight : MonoBehaviour
{
    [SerializeField] private float flightSpeed = 1.0f;
    [SerializeField] private float curveHeight = 3.0f;


    public void FlyArrow(Vector3 startPoint, Vector3 endPoint, float enemyRadius, TweenCallback OnArrowHit)
    {
        transform.position = startPoint;
        startPoint = transform.localPosition;

        // Calculate the control point for the curve
        Vector3 controlPoint = (transform.localPosition + endPoint) / 2 + Vector3.up * curveHeight;

        // Use DOTween to create a smooth curved path
        Vector3[] path = new Vector3[] { transform.localPosition, controlPoint, endPoint };

        Vector3 previousPosition = transform.localPosition;
        float distance = (endPoint - startPoint).magnitude;
        float flightDuration = distance / flightSpeed;

        Sequence seq = DOTween.Sequence();

        // Move the arrow along the path and update its rotation
        seq.Append(transform.DOLocalPath(path, flightDuration, PathType.CatmullRom, PathMode.Full3D)
            .OnUpdate(() =>
            {
                // Smoothly rotate the arrow to face its movement direction
                Vector3 direction = (transform.localPosition - previousPosition).normalized;
                previousPosition = transform.localPosition;
                if (direction != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(direction);
                }
            }));
        
        // Calculate the time at which the arrow should hit the target based on its size
        seq.InsertCallback(flightDuration * (distance - enemyRadius - 1) / distance, OnArrowHit);
    }
}