using UnityEngine;

namespace Enter
{
  [DisallowMultipleComponent]
  public class PlayerManager : MonoBehaviour
  {
    public static PlayerManager Instance;

    [SerializeField] private GameObject _playerPrefab;

    #region ================== Accessors

    public static GameObject   Player => PlayerScript.Instance.gameObject;
    public static PlayerScript PlayerScript => PlayerScript.Instance;
    public static Rigidbody2D  PlayerRigidbody => PlayerScript.Instance.Rigidbody2D;

    #endregion

    #region ================== Methods

    void Awake()
    {
      Instance = this;
    }

    void Start()
    {
      if (!PlayerScript.Instance)
      {
        Instantiate(_playerPrefab, SceneTransitioner.Instance.SpawnPosition, Quaternion.identity);
      }

      Player.transform.SetParent(null);
      DontDestroyOnLoad(Player);
    }

    #endregion
  }
}