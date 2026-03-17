using UnityEditor;
using UnityEngine;

namespace UniCore.Editor.PrefsEditor
{
    public class ImportPrefsWizard : ScriptableWizard
    {
        [SerializeField] private string m_importCompanyName = "";
        [SerializeField] private string m_importProductName = "";

        private void OnEnable()
        {
            m_importCompanyName = PlayerSettings.companyName;
            m_importProductName = PlayerSettings.productName;
        }

        private void OnInspectorUpdate()
        {
            if (Resources.FindObjectsOfTypeAll(typeof(PrefsEditor)).Length == 0)
            {
                Close();
            }
        }

        protected override bool DrawWizardGUI()
        {
            GUILayout.Label("Import PlayerPrefs from another project, also useful if you change product or company name", EditorStyles.wordWrappedLabel);
            EditorGUILayout.Separator();
            return base.DrawWizardGUI();
        }

        private void OnWizardCreate()
        {
            if (Resources.FindObjectsOfTypeAll(typeof(PrefsEditor)).Length >= 1)
            {
                ((PrefsEditor)Resources.FindObjectsOfTypeAll(typeof(PrefsEditor))[0]).Import(m_importCompanyName, m_importProductName);
            }
        }
    }
}