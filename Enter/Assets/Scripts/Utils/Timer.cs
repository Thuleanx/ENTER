using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Enter.Utils {
    /**
     * Syntactic fluff around a float value meant to represent when a timer
     * expires.
     */
    public struct Timer {
        public float Duration { get; private set; }

        public float TimeLeft {
            get => Mathf.Max(_expirationTime - Time.time, 0);
            set => _expirationTime = value + Time.time;
        }
        // useful for making cooldown sliders or something
        public float ElapsedFraction { get => Duration > 0 ? 1 - TimeLeft / Duration : 0; }
        private float _expirationTime;

        /**
         * @brief Construct a timer with a certain duration. By default, this timer is paused.
         */
        public Timer(float durationSeconds) {
            Duration = durationSeconds;
            _expirationTime = Time.time + durationSeconds;
        }

        public Timer Start() {
            TimeLeft = Duration;
            return this;
        }
        public Timer Stop() {
            TimeLeft = 0;
            return this;
        }

        // handy operator to implicitly convert the timer to a bool, so you can do if (timer) to see if it's still active
        public static implicit operator bool(Timer timer) => timer.TimeLeft > 0;
        public static implicit operator Timer(bool value) => new Timer(value ? 1 : 0);
        public static implicit operator Timer(float durationSeconds) => new Timer(durationSeconds).Start();
    }
}
