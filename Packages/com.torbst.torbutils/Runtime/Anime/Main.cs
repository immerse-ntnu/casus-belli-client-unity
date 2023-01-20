using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace TorbuTils
{
    namespace Anime
    {
        internal class Main : MonoBehaviour
        {
            internal static Main Instance { get; private set; }

            private void Awake()
            {
                Instance = this;
            }

            internal void Begin<T>(Anim<T> anim)
            {
                FindController<T>().Begin(anim);
            }
            internal void Stop<T>(Anim<T> anim, float stopAtTime)
            {
                FindController<T>().Stop(anim, stopAtTime);
            }
            private static AnimController<T> FindController<T>()
            {
                AnimController<T> controller = Instance.GetComponent<AnimController<T>>();
                if (controller == null)
                    controller = Instance.GetComponentInChildren<AnimController<T>>();
                if (controller == null) throw new InvalidOperationException(Instance.gameObject + " and its children do not have an animation controller for the type " + typeof(T));
                return controller;
            }
        }
        public abstract class AnimController<T> : MonoBehaviour
        {
            private readonly List<Anim<T>> anims = new();
            private int count;

            public void Begin(Anim<T> anim)
            {
                anims.Add(anim);
                count = anims.Count;
            } 
            public void Stop(Anim<T> anim, float stopAtRelative)
            {
                int i = anims.FindIndex(x => x == anim);
                if (i == -1)
                {
                    Debug.LogWarning("Tried stopping an unregistered animation: "+anim);
                } else
                {
                    stopAtRelative = Mathf.Clamp(stopAtRelative, 0f, 1f);
                    float curveAdjusted = GetCurveAdjusted(anim.Curve, stopAtRelative);
                    anim.Action(GetActualValue(anim.StartValue, anim.EndValue, stopAtRelative));
                    anims.RemoveAt(i);
                    count = anims.Count;
                    anim.Finished();
                }
            }
            void Update()
            {
                for (int i = anims.Count - 1; i >= 0; i--)
                {
                    Anim<T> anim = anims[i];
                    float timePassed = Time.time - anim.StartTime;
                    float relative = (timePassed / anim.Duration);
                    bool end = false;
                    if (relative >= 1f)
                    {
                        relative = 1f;
                        end = true;
                    }

                    float curveAdjusted = GetCurveAdjusted(anim.Curve, relative);

                    T startValue = anim.StartValue;
                    T endValue = anim.EndValue;
                    T actualValue = GetActualValue(startValue, endValue, curveAdjusted);

                    anim.Action(actualValue);
                    if (end)
                    {
                        anims.RemoveAt(i);
                        count = anims.Count;
                        anim.Finished();
                    }
                }
            }
            private float GetCurveAdjusted(AnimationCurve curve, float relative)
            {
                if (curve == null) return relative;
                else return curve.Evaluate(relative);
            }
            protected abstract T GetActualValue(T startValue, T endValue, float time);
        }
        
        public delegate void BasicAction<T>(T value);
        public class Anim<T>
        {
            // INFO
            public float Progress => (Time.time - StartTime) / Duration;

            // REQUIRE ASSIGNMENT
            public T StartValue { get; set; }
            public T EndValue { get; set; }
            public BasicAction<T> Action { get; set; }

            // OPTIONAL ASSIGNMENT
            public event Action<Anim<T>> OnFinish;
            public AnimationCurve Curve { get; set; }
            public GameObject GO { get; set; }
            public float Duration { get; set; } = 1f;
            public float StartTime { get; set; } = Time.time;

            public void Start()
            {
                Main.Instance.Begin(this);
            }
            public void Stop(float? stopAtTime = null)
            {
                // Default is: stop at current position.
                // 0: go to start of animation
                // 1: go to end of animation
                if (stopAtTime == null) stopAtTime = Progress;
                Main.Instance.Stop(this, (float)stopAtTime);
            }

            internal void Finished()
            {
                // OH BOY this is bad
                OnFinish?.Invoke(this);
            }
        }
    }
}