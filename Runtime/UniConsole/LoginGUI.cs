using UnityEngine;

namespace UniConsole
{
    public class LoginGUI : MonoBehaviour
    {
        private ConsoleSettings _settings;
        private ControlTrigger _controlTrigger;
        private string _inputPassword = "";

        public void Initialize(ConsoleSettings settings)
        {
            _settings = settings;
            _controlTrigger = new ControlTrigger(settings.m_loginTriggerMode, settings.m_loginTapCount, settings.m_loginTapTimeout, settings.m_loginLongPressDuration);
        }

        public void Open()
        {
            _controlTrigger.IsOpen = true;
            _inputPassword = "";
        }
        
        private void Update()
        {
            if (_controlTrigger.CheckTriggers() == TriggerResult.Request)
            {
                Open();
            }
        }

        private void OnGUI()
        {
            if (!_controlTrigger.IsOpen) return;
            var baseScale = Mathf.Min(Screen.width, Screen.height) / _settings.m_referenceMinDimension;
            var finalScale = baseScale * _settings.m_guiScaleMultiplier;

            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(finalScale, finalScale, 1f));
            var virtualWidth = Screen.width / finalScale;
            var virtualHeight = Screen.height / finalScale;
            DrawLoginPanel(virtualWidth, virtualHeight, new GUIStyle(GUI.skin.label) { fontSize = 16, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter });
        }

        public void DrawLoginPanel(float virtualWidth, float virtualHeight, GUIStyle titleStyle)
        {
            var rect = new Rect(virtualWidth / 2f - 200, virtualHeight / 2f - 100, 400, 200);
            GUILayout.BeginArea(rect, GUI.skin.box);
            GUILayout.Label("Developer Authentication", titleStyle);

            _inputPassword = GUILayout.PasswordField(_inputPassword, '*', GUILayout.Height(40));
            GUILayout.Space(10);

            if (GUILayout.Button("Login", GUILayout.Height(50)))
            {
                if (_inputPassword == _settings.m_password)
                {
                    DeveloperAuthenticator.IsDeveloperMode = true;
                    PlayerPrefs.SetInt("unicore_flag", 1); 
                    PlayerPrefs.Save();
      
                    DeveloperAuthenticator.OpenConsole(); 
                    
                    Destroy(this);
                }

                _inputPassword = "";
            }

            GUILayout.Space(10);
            if (GUILayout.Button("Close", GUILayout.Height(50)))
            {
                _controlTrigger.IsOpen = false;
            }

            GUILayout.EndArea();
        }
    }
}