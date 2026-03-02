#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace UniCore.Utilities
{
    public abstract class UniSettingsEditorBase<T1, T2>
#if UNITY_EDITOR
        : ScriptableSingleton<T1> where T1 : UniSettingsEditorBase<T1, T2>
#endif
    {
#if UNITY_EDITOR
        [SerializeField] internal T2 EditorData;

        internal void SaveData()
        {
            Save(true);
        }
#endif
    }
}