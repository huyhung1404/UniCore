using UnityEngine;

namespace UniCore.Notify.DotPing.Internal
{
    internal class ShakeScaleGraphic : PingGraphic
    {
        [SerializeField] private float shakeDuration = 0.5f;
        [SerializeField] private float strength = 0.3f;
        [SerializeField] private float interval = 2.5f;
        [SerializeField] private int vibrato = 20;

        private float timer;
        private bool shaking;
        private Vector3 baseScale;

        private void OnEnable()
        {
            baseScale = Vector3.one;
            transform.localScale = baseScale;
            timer = 0f;
            shaking = true;
        }

        private void Update()
        {
            timer += Time.deltaTime;

            if (shaking)
            {
                var t = timer / shakeDuration;

                if (t >= 1f)
                {
                    shaking = false;
                    timer = 0f;
                    transform.localScale = baseScale;
                    return;
                }

                var fade = 1f - t;

                var freq = vibrato / shakeDuration;
                var noiseX = Mathf.PerlinNoise(Time.time * freq, 0f) - 0.5f;
                var noiseY = Mathf.PerlinNoise(0f, Time.time * freq) - 0.5f;

                var offset = new Vector3(noiseX, noiseY, 0f) * (strength * fade);
                transform.localScale = baseScale + offset;
            }
            else
            {
                if (timer >= interval)
                {
                    shaking = true;
                    timer = 0f;
                }
            }
        }

        private void OnDisable()
        {
            transform.localScale = baseScale;
        }
    }
}