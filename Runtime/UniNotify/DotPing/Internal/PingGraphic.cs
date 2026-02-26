using System.Collections.Generic;
using UnityEngine;

namespace UniCore.Notify.DotPing.Internal
{
    public abstract class PingGraphic : MonoBehaviour
    {
        [SerializeField] internal GraphicType m_graphicType;
        private static readonly Dictionary<GraphicType, Queue<PingGraphic>> s_pool = new Dictionary<GraphicType, Queue<PingGraphic>>(2);

        internal void ReturnToPool()
        {
            if (!s_pool.ContainsKey(m_graphicType))
            {
                s_pool[m_graphicType] = new Queue<PingGraphic>(5);
            }

            s_pool[m_graphicType].Enqueue(this);
            transform.SetParent(PingSystem.GraphicParent());
            gameObject.SetActive(false);
        }

        internal static PingGraphic GetOrCreate(GraphicType type, Transform parent)
        {
            PingGraphic pingGraphic;
            if (!s_pool.TryGetValue(type, out var queue) || queue.Count <= 0)
            {
                pingGraphic = PingSystem.CreateNewPingGraphic(type);
            }
            else
            {
                pingGraphic = queue.Dequeue();
            }

            pingGraphic.transform.SetParent(parent);
            pingGraphic.transform.localScale = Vector3.one;
            pingGraphic.transform.localPosition = Vector3.zero;
            pingGraphic.gameObject.SetActive(true);
            return pingGraphic;
        }
    }
}