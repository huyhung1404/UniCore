using UnityEngine;

namespace UniCore.Notify.DotPing.Internal
{
    public class JumpGraphic : PingGraphic
    {
        [SerializeField] private float m_jumpPower = 15f;
        [SerializeField] private int m_jumpCount = 2;
        [SerializeField] private float m_jumpDuration = 1f;
        [SerializeField] private float m_interval = 2.5f;

        private Vector3 _startPos;
        private float _timer;
        private bool _jumping;

        private void OnEnable()
        {
            _startPos = transform.localPosition;
            transform.localScale = Vector3.one;
            _timer = 0f;
            _jumping = true;
        }

        private void Update()
        {
            _timer += Time.deltaTime;

            if (_jumping)
            {
                var t = _timer / m_jumpDuration;

                if (t >= 1f)
                {
                    _jumping = false;
                    _timer = 0f;
                    transform.localPosition = _startPos;
                    return;
                }

                var jumpT = (t * m_jumpCount) % 1f;
                var height = 4f * m_jumpPower * jumpT * (1f - jumpT);

                transform.localPosition = _startPos + Vector3.up * height;
            }
            else
            {
                if (_timer >= m_interval)
                {
                    _jumping = true;
                    _timer = 0f;
                }
            }
        }

        private void OnDisable()
        {
            transform.localPosition = _startPos;
        }
    }
}