using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Assertions;

namespace Enter
{
  [DisallowMultipleComponent]
  public class TimerManager : MonoBehaviour
  {
    public static TimerManager Instance;

    [SerializeField] private TextMeshProUGUI _timerDisplay;

    private float currentTime;
    public bool Paused = true;

    void Awake()
    {
      Instance = this;
      Assert.IsNotNull(_timerDisplay, "TimerManager must have a reference to its the timer's canvas.");
      currentTime = 0;
    }

    void Start() {
        // at first, no need to display the time.
        _timerDisplay.gameObject.SetActive(false);
    }

    void Update() {
        if (!Paused && !_timerDisplay.gameObject.activeSelf)
            _timerDisplay.gameObject.SetActive(true);
    }

    void LateUpdate()
    {
        if (!Paused) currentTime += Time.deltaTime;
        setDisplayRunTime(currentTime);
    }

    private void setDisplayRunTime(float x)
    {
      float minutes = Mathf.FloorToInt(x / 60); 
      float seconds = Mathf.FloorToInt(x % 60);
      float milliseconds = (x % 1) * 1000;
      _timerDisplay.text = string.Format("{0:00}:{1:00}:{2:000}\nDeaths: {3}", minutes, seconds, milliseconds, PlayerManager.DeathCount);
    }
  }
}
