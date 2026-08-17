using UnityEngine;

namespace toolbox.Samples.AbilitySystemDemo
{
    /// <summary>Fixed-offset follow camera; nothing ability-system related.</summary>
    public class SampleCameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset = new(0f, 8f, -8f);
        [SerializeField] private float smoothing = 8f;

        private void LateUpdate()
        {
            if (target == null)
                return;

            transform.position = Vector3.Lerp(transform.position, target.position + offset, smoothing * Time.deltaTime);
            transform.rotation = Quaternion.LookRotation(target.position + Vector3.up - transform.position);
        }
    }
}
