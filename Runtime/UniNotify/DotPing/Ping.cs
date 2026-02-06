using System;
using UniCore.Notify.DotPing.Internal;
using UnityEngine;

namespace UniCore.Notify.DotPing
{
    public class Ping : MonoBehaviour
    {
        [SerializeField] internal GraphicType graphicType;
        [SerializeField] protected string locationId;
        private PingGraphic pingGraphic;

        public void ChangeLocationId(string value)
        {
            if (CompareSpan(value.AsSpan(), locationId.AsSpan())) return;
            locationId = value;
            OnValueChange(PingSystem.IsActive(locationId));
        }

        private void OnEnable()
        {
            PingSystem.OnPingChanged += HandlePingChange;
            OnValueChange(PingSystem.IsActive(locationId));
        }

        private void OnDisable()
        {
            PingSystem.OnPingChanged -= HandlePingChange;
        }

        private void HandlePingChange(string id, bool isActive)
        {
            if (CompareSpan(id.AsSpan(), locationId.AsSpan())) OnValueChange(isActive);
        }

        internal void OnValueChange(bool isActive)
        {
            if (ReferenceEquals(pingGraphic, null))
            {
                if (!isActive) return;
                pingGraphic = PingGraphic.GetOrCreate(graphicType, transform);
                return;
            }

            if (isActive) return;
            pingGraphic.ReturnToPool();
            pingGraphic = null;
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