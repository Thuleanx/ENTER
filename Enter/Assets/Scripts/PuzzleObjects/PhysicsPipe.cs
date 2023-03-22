using UnityEngine;
using UnityEngine.Events;
using NaughtyAttributes;

namespace Enter
{
  public class PhysicsPipe : MonoBehaviour
  {
    [SerializeField] private float _blocksPerMinute;
    [ShowAssetPreview, SerializeField] GameObject physicsObjectPrefab;
	[SerializeField] UnityEvent OnSpawn;
    float timeLastSpawn = -1;

    #region ================== Methods

    void Update()
    {
      if (timeLastSpawn + 60f / _blocksPerMinute < Time.time)
      {
        CreatePhysicsObject();
		OnSpawn?.Invoke();
        timeLastSpawn = Time.time;
      }
    }

    #endregion

    #region ================== Methods

    // sadguh no object pool
    private void CreatePhysicsObject()
    {
      Instantiate(physicsObjectPrefab, transform.position, Quaternion.identity);
    }

    #endregion
  }
}