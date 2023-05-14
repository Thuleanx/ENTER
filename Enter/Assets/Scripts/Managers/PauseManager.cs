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

    bool audioPrepped = false;

    struct Bus {
        public int current { get; private set; }
        FMOD.Studio.Bus bus;

        public Bus(string busName) {
            current = 10;
            bus = FMODUnity.RuntimeManager.GetBus(busName);
        }
        
        public float getVolume() {
            float amt;
            if (bus.getVolume(out amt) != FMOD.RESULT.OK)
            {
                Debug.LogError("Cannot get volume for bus //Master//Music");
                return 0;
            }
            return amt;
        }

        void setVolume(float db) {
            bus.setVolume(db);
        }

        public void setVolume(int value, float minDb, float maxDb, int maxVal) {
            current = Mathf.Clamp(value, 0, maxVal);
            float amt = Mathf.Lerp(minDb, maxDb, ((float) value) / maxVal);
            setVolume(amt);
        }
    };

    [SerializeField] private int   _maxUserVol = 10;
    private int   _currentMusicVol, _currentSFXVol;
    private float _maxTrueVol = 1, _minTrueVol = 0;

    private Bus _musicBus;
    private Bus _sfxBus;
    private float _startingMusicVol;
    private float _startingSFXVol;

    #region ================== Methods

    void Awake()
    {
        Instance = this;
        Assert.IsNotNull(_pauseMenu, "PauseManager must have a reference to its the pause menu's canvas.");
        _pauseMenu.SetActive(false);
        _settingsMenu.SetActive(false);
    }

    void InitAudio() {
        _musicBus = new Bus("bus:/Master/Music");
        _sfxBus = new Bus("bus:/Master/SFX");
        audioPrepped = true;
        UpdateVolumeText();
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
        if (!audioPrepped) InitAudio();
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

    void UpdateVolumeText() {
        _mscVolumeText.text = _musicBus.current.ToString();
        _sfxVolumeText.text = _sfxBus.current.ToString();
    }

    public void MusicVolumeUp() {
        if (!audioPrepped) InitAudio();
        _musicBus.setVolume(_musicBus.current + 1, _minTrueVol, _maxTrueVol, _maxUserVol);
        UpdateVolumeText();
    }

    public void MusicVolumeDown()
    {
        if (!audioPrepped) InitAudio();
        _musicBus.setVolume(_musicBus.current - 1, _minTrueVol, _maxTrueVol, _maxUserVol);
        UpdateVolumeText();
    }

    public void SFXVolumeUp()
    {
        if (!audioPrepped) InitAudio();
        _sfxBus.setVolume(_sfxBus.current + 1, _minTrueVol, _maxTrueVol, _maxUserVol);
        UpdateVolumeText();
    }

    public void SFXVolumeDown()
    {
        if (!audioPrepped) InitAudio();
        _sfxBus.setVolume(_sfxBus.current - 1, _minTrueVol, _maxTrueVol, _maxUserVol);
        UpdateVolumeText();
    }

    #endregion

    #endregion
}
}
