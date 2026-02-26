using UnityEngine;

namespace UniCore.Notify.DotPing.Internal
{
    internal class ShakeScaleGraphic : PingGraphic
    {
        [SerializeField] private float m_shakeDuration = 0.5f;
        [SerializeField] private float m_strength = 0.3f;
        [SerializeField] private float m_interval = 2.5f;
        [SerializeField] private int m_vibrato = 20;

        private float _timer;
        private bool _shaking;
        private Vector3 _baseScale;

        private void OnEnable()
        {
            _baseScale = Vector3.one;
            transform.localScale = _baseScale;
            _timer = 0f;
            _shaking = true;
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            if (_shaking)
            {
                var t = _timer / m_shakeDuration;

                if (t >= 1f)
                {
                    _shaking = false;
                    _timer = 0f;
                    transform.localScale = _baseScale;
                    return;
                }

                var fade = 1f - t;

                var freq = m_vibrato / m_shakeDuration;
                var noiseX = Mathf.PerlinNoise(Time.time * freq, 0f) - 0.5f;
                var noiseY = Mathf.PerlinNoise(0f, Time.time * freq) - 0.5f;

                var offset = new Vector3(noiseX, noiseY, 0f) * (m_strength * fade);
                transform.localScale = _baseScale + offset;
            }
            else
            {
                if (_timer >= m_interval)
                {
                    _shaking = true;
                    _timer = 0f;
                }
            }
        }

        private void OnDisable()
        {
            transform.localScale = _baseScale;
        }
    }
}