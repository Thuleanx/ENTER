using UnityEngine;

namespace Enter
{
  public class DisableAfterLifetime : MonoBehaviour
  {
    [SerializeField, Range(0,30)] public float LifetimeSeconds;
    float startTime;

    private void OnEnable()
    {
      startTime = Time.time;
    }

    private void Update()
    {
      if (startTime + LifetimeSeconds < Time.time) gameObject.SetActive(false);
    }
  }
}
