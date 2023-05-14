using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using TMPro;

namespace Enter
{
  [DisallowMultipleComponent]
  public class PauseManager : MonoBehaviour
  {
    public static PauseManager Instance;

    [SerializeField] private GameObject _pauseMenuBackground;
    [SerializeField] private GameObject _pauseMenu;
    [SerializeField] private GameObject _settingsMenu;
    [SerializeField] private TextMeshProUGUI _mscVolumeText;
    [SerializeField] private TextMeshProUGUI _sfxVolumeText;

    private float _prevTimeScale = 1;

    public bool IsPaused => _pauseMenu && _pauseMenu.activeSelf;

    FMOD.Studio.Bus Music;
		FMOD.Studio.Bus SFX;
		FMOD.Studio.Bus Master;
		bool audioPrepped = false;
    private int   _maxUserVol = 10, _minUserVol = 0;
    private float _maxTrueVol = 1f, _minTrueVol = 0f;
    private float volIncrement => (float) (_maxTrueVol - _minTrueVol) / (float) (_maxUserVol - _minUserVol);

    #region ================== Methods

    void Awake()
    {
      Instance = this;
      Assert.IsNotNull(_pauseMenu, "PauseManager must have a reference to its the pause menu's canvas.");
      _pauseMenu.SetActive(false);
      _settingsMenu.SetActive(false);
    }

    void InitAudio() {
      Music = FMODUnity.RuntimeManager.GetBus("bus:/Music");
			SFX = FMODUnity.RuntimeManager.GetBus("bus:/SFX");
			// Master = FMODUnity.RuntimeManager.GetBus("bus:/Master");
      audioPrepped = true;
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

      _pauseMenuBackground.SetActive(true);
      _pauseMenu.SetActive(true);
      _settingsMenu.SetActive(false);
      _prevTimeScale = Time.timeScale;
      Time.timeScale = 0;
      _mscVolumeText.text = TrueVolToUser(GetMusicVolume()).ToString();
      _sfxVolumeText.text = TrueVolToUser(GetSFXVolume()).ToString();
    }

    public void Unpause()
    {
      if (!IsPaused) return;

      _pauseMenuBackground.SetActive(false);
      _pauseMenu.SetActive(false);
      _settingsMenu.SetActive(false);
      Time.timeScale = _prevTimeScale;
    }

     public void ShowSettings()
    {
      _settingsMenu.SetActive(true);
      _pauseMenu.SetActive(false);
    }

    public void HideSettings()
    {
      _settingsMenu.SetActive(false);
      _pauseMenu.SetActive(true);
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
      CursorManager.Instance.HoveringEntities.Add(gameObject);;
    }

    public void CursorNormal()
    {
      CursorManager.Instance.HoveringEntities.Remove(gameObject);
    }

    #region ================== Volume

    private int TrueVolToUser(float newTrueVol)
    {
      return ((int) (((newTrueVol - _minTrueVol) / volIncrement) + _minUserVol + .5f));
    }

    public void MusicVolumeUp()
    {
      float currTrueVol = GetMusicVolume();
      float newTrueVol = currTrueVol + volIncrement;
      newTrueVol = Mathf.Clamp(newTrueVol, _minTrueVol, _maxTrueVol);
      _mscVolumeText.text = TrueVolToUser(newTrueVol).ToString();
      SetMusicVolume(newTrueVol);
    }

    public void MusicVolumeDown()
    {
      float currTrueVol = GetMusicVolume();
      float newTrueVol = currTrueVol - volIncrement;
      newTrueVol = Mathf.Clamp(newTrueVol, _minTrueVol, _maxTrueVol);
      _mscVolumeText.text = TrueVolToUser(newTrueVol).ToString();
      SetMusicVolume(newTrueVol);
    }

    public void SFXVolumeUp()
    {
      float currTrueVol = GetSFXVolume();
      float newTrueVol = currTrueVol + volIncrement;
      newTrueVol = Mathf.Clamp(newTrueVol, _minTrueVol, _maxTrueVol);
      _sfxVolumeText.text = TrueVolToUser(newTrueVol).ToString();
      SetSFXVolume(newTrueVol);
    }

    public void SFXVolumeDown()
    {
      float currTrueVol = GetSFXVolume();
      float newTrueVol = currTrueVol - volIncrement;
      newTrueVol = Mathf.Clamp(newTrueVol, _minTrueVol, _maxTrueVol);
      _sfxVolumeText.text = TrueVolToUser(newTrueVol).ToString();
      SetSFXVolume(newTrueVol);
    }

    public float GetMusicVolume()
    {
			if (!audioPrepped) InitAudio();
			float amt;
			if (Music.getVolume(out amt) != FMOD.RESULT.OK)
      {
				Debug.LogError("Cannot get volume for bus //Master//Music");
				return 0;
			}
			return amt;
		}

		public float GetMasterVolume()
    {
			if (!audioPrepped) InitAudio();
			float amt;
			if (Master.getVolume(out amt) != FMOD.RESULT.OK)
      {
				Debug.LogError("Cannot get volume for bus //Master");
				return 0;
			}
			return amt;
		}

		public float GetSFXVolume()
    {
			if (!audioPrepped) InitAudio();
			float amt;
			if (SFX.getVolume(out amt) != FMOD.RESULT.OK)
      {
				Debug.LogError("Cannot get volume for bus //Master//SFX");
				return 0;
			}
			return amt;
		}

		public void SetMusicVolume(float amt)
    {
			if (!audioPrepped) InitAudio();
			Music.setVolume(amt);
		}

		public void SetSFXVolume(float amt)
    {
			if (!audioPrepped) InitAudio();
			SFX.setVolume(amt);
		}

		public void SetMasterVolume(float amt)
    {
			if (!audioPrepped) InitAudio();
			Master.setVolume(amt);
		}

    #endregion

    #endregion
  }
}