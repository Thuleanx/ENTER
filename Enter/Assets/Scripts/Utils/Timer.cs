using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enter.Utils {
    /**
     * Class that represents a timer.
     */
    public struct Timer {
        public float Duration { get; private set; }
        public bool Paused { get; private set; }
        float timeLeftLagged;

        // The timer itself doesn't really need updating every frame
        // Instead, every time the user requests for its time, it calculates how much time 
        // is left by looking at the current duration and time the user last requested its time.
        public float TimeLeft {
            get {
                if (!Paused)
                    timeLeftLagged = Mathf.Max(timeLeftLagged - (Time.time - TimeLastSampled), 0);
                TimeLastSampled = Time.time;
                return timeLeftLagged;
            }
            set {
                timeLeftLagged = value;
                TimeLastSampled = Time.time;
            }
        }
        public float TimeLastSampled { get; private set; }
        public float ElapsedFraction { get => 1 - TimeLeft / Duration; }

        /**
         * @brief Construct a timer with a certain duration. By default, this timer is paused.
         */
        public Timer(float durationSeconds, bool pausedDefault = true) {
            Duration = durationSeconds;
            timeLeftLagged = 0;
            TimeLastSampled = Time.time;
            Paused = pausedDefault;
            if (!pausedDefault) Start();
        }

        public void Start() {
            TimeLeft = Duration;
            Paused = false;
        }
        public void Pause() {
            float left = TimeLeft;
            Paused = true;
        }
        public void UnPause() {
            float left = TimeLeft;
            Paused = false;
        }
        public void Stop() { TimeLeft = 0; }

        // handy operator to implicitly convert the timer to a bool, so you can do if (timer) to see if it's still active
        public static implicit operator bool(Timer timer) => timer.TimeLeft > 0;
        public static implicit operator Timer(float durationSeconds) => new Timer(durationSeconds, false);
    }
}
