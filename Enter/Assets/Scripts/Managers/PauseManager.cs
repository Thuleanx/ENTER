using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

namespace Enter
{
  [DisallowMultipleComponent]
  public class PauseManager : MonoBehaviour
  {
    public static PauseManager Instance;

    [SerializeField] private GameObject _pauseMenuCanvas;

    private float _prevTimeScale = 1;

    public bool IsPaused => _pauseMenuCanvas && _pauseMenuCanvas.activeSelf;

    #region ================== Methods

    void Awake()
    {
      Instance = this;
      Assert.IsNotNull(_pauseMenuCanvas, "PauseManager must have a reference to its the pause menu's canvas.");
      _pauseMenuCanvas.SetActive(false);
    }

    public void TogglePause()
    {
      if (IsPaused) Unpause();
      else          Pause();
    }

    public void Pause()
    {
      if (IsPaused) return;

      if (SceneTransitioner.Instance && SceneTransitioner.Instance.STState != STState.Idle) return;

      _pauseMenuCanvas.SetActive(true);
      _prevTimeScale = Time.timeScale;
      Time.timeScale = 0;
    }

    public void Unpause()
    {
      if (!IsPaused) return;

      _pauseMenuCanvas.SetActive(false);
      Time.timeScale = _prevTimeScale;
    }

    public void ShowSettings()
    {
      Debug.Log("ShowSettings");
    }

    public void RestartLevel()
    {
      SceneTransitioner.Instance.Reload();
    }

    public void QuitGame()
    {
      Debug.Log("QuitGame");
    }

    public void CursorHover()
    {
      CursorManager.Instance.SetCursor(CursorManager.CursorType.Hover);
    }

    public void CursorNormal()
    {
      CursorManager.Instance.SetCursor(CursorManager.CursorType.Pointer);
    }

    #endregion
  }
}