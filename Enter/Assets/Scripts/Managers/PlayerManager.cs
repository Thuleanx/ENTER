using UnityEngine;

namespace Enter
{
  [DisallowMultipleComponent]
  public class PlayerManager : MonoBehaviour
  {
    public static PlayerManager Instance;

    [SerializeField] private GameObject _playerPrefab;

    #region ================== Accessors

    public static PlayerScript PlayerScript   => PlayerScript.Instance;
    public static GameObject   Player         => PlayerScript.Instance.gameObject;
    public static bool         PlayerGrounded => PlayerScript.Instance.Grounded;

    public static Rigidbody2D           Rigidbody             => PlayerScript.Instance.Rigidbody2D;
    public static BoxCollider2D         BoxCollider           => PlayerScript.Instance.BoxCollider2D;
    public static PlayerStretcherScript PlayerStretcherScript => PlayerScript.Instance.PlayerStretcherScript;
    public static PlayerColliderScript  PlayerColliderScript  => PlayerScript.Instance.PlayerColliderScript;
    public static SpriteRenderer        SpriteRenderer        => PlayerScript.Instance.SpriteRenderer;
    public static Animator              Animator              => PlayerScript.Instance.Animator;
    public static float                 MaxJumpSpeed          => PlayerScript.Instance.MaxJumpSpeed;
    public static float                 MaxFallSpeed          => PlayerScript.Instance.MaxFallSpeed;

    public static int DeathCount => PlayerScript.Instance.DeathCount;

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