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

    private float _startTime;

    void Awake()
    {
      Instance = this;
      Assert.IsNotNull(_timerDisplay, "TimerManager must have a reference to its the timer's canvas.");

      _startTime = Time.time;
    }

    void LateUpdate()
    {
      setDisplayRunTime(Time.time - _startTime);
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