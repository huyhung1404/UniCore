using System;
using UniCore.Notify.DotPing.Internal;
using UnityEngine;

namespace UniCore.Notify.DotPing
{
    public class Ping : MonoBehaviour
    {
        [SerializeField] internal GraphicType m_graphicType;
        [SerializeField] protected string m_locationId;
        private PingGraphic _pingGraphic;

        public void ChangeLocationId(string value)
        {
            if (CompareSpan(value.AsSpan(), m_locationId.AsSpan())) return;
            m_locationId = value;
            OnValueChange(PingSystem.IsActive(m_locationId));
        }

        private void OnEnable()
        {
            PingSystem.OnPingChanged += HandlePingChange;
            OnValueChange(PingSystem.IsActive(m_locationId));
        }

        private void OnDisable()
        {
            PingSystem.OnPingChanged -= HandlePingChange;
        }

        private void HandlePingChange(string id, bool isActive)
        {
            if (CompareSpan(id.AsSpan(), m_locationId.AsSpan())) OnValueChange(isActive);
        }

        internal void OnValueChange(bool isActive)
        {
            if (ReferenceEquals(_pingGraphic, null))
            {
                if (!isActive) return;
                _pingGraphic = PingGraphic.GetOrCreate(m_graphicType, transform);
                return;
            }

            if (isActive) return;
            _pingGraphic.ReturnToPool();
            _pingGraphic = null;
        }

        private static bool CompareSpan(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
        {
            if (a.Length != b.Length) return false;

            for (var i = 0; i < a.Length; i++)
            {
                if (a[i] != b[i]) return false;
            }

            return true;
        }
    }
}