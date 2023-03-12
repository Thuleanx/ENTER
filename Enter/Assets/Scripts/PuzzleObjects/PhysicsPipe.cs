using UnityEngine;
using NaughtyAttributes;

namespace Enter
{
  public class PhysicsPipe : MonoBehaviour
  {
    [SerializeField] private float _blocksPerMinute;
    [ShowAssetPreview, SerializeField] GameObject physicsObjectPrefab;
    float timeSinceLastSpawn = -1;

    #region ================== Methods

    void Update()
    {
      if (timeSinceLastSpawn + 60f / _blocksPerMinute < Time.time)
      {
        CreatePhysicsObject();
        timeSinceLastSpawn = Time.time;
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