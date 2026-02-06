using UnityEngine;

namespace UniCore.Notify.DotPing.Internal
{
    public class JumpGraphic : PingGraphic
    {
        [SerializeField] private float jumpPower = 15f;
        [SerializeField] private int jumpCount = 2;
        [SerializeField] private float jumpDuration = 1f;
        [SerializeField] private float interval = 2.5f;

        private Vector3 startPos;
        private float timer;
        private bool jumping;

        private void OnEnable()
        {
            startPos = transform.localPosition;
            transform.localScale = Vector3.one;
            timer = 0f;
            jumping = true;
        }

        private void Update()
        {
            timer += Time.deltaTime;

            if (jumping)
            {
                var t = timer / jumpDuration;

                if (t >= 1f)
                {
                    jumping = false;
                    timer = 0f;
                    transform.localPosition = startPos;
                    return;
                }

                var jumpT = (t * jumpCount) % 1f;
                var height = 4f * jumpPower * jumpT * (1f - jumpT);

                transform.localPosition = startPos + Vector3.up * height;
            }
            else
            {
                if (timer >= interval)
                {
                    jumping = true;
                    timer = 0f;
                }
            }
        }

        private void OnDisable()
        {
            transform.localPosition = startPos;
        }
    }
}