using UnityEngine;

namespace UniCore.Utilities
{
    public abstract class UniSettingsBase<T1, T2, T3> : ScriptableObject 
        where T1 : UniSettingsBase<T1, T2, T3> 
        where T3 : UniSettingsEditorBase<T3, T2>
    {
        [SerializeField] protected T2 Data;

        public static T1 GetInstance(string fileName)
        {
            var instance = Resources.Load<T1>(fileName);
            if (instance != null) return instance;

            instance = CreateInstance<T1>();

#if UNITY_EDITOR
            if (UniSettingsEditorBase<T3, T2>.instance != null)
            {
                instance.Data = UniSettingsEditorBase<T3, T2>.instance.EditorData;
            }
#endif

            return instance;
        }

        internal void SetData(T2 data)
        {
            Data = data;
        }
    }
}