using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.InputSystem;
using System.Collections;
using System.Collections.Generic;

namespace Enter
{
  [DisallowMultipleComponent]
  public class PlayerManager : MonoBehaviour
  {
    public static PlayerManager Instance;

    [SerializeField] private GameObject _playerPrefab;

    #region ================== Accessors

    public GameObject   Player       => PlayerScript.Instance.gameObject;
    public PlayerScript PlayerScript => PlayerScript.Instance;

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
      
      PlayerScript.transform.SetParent(null);
      DontDestroyOnLoad(Player);
    }

    #endregion
  }
}