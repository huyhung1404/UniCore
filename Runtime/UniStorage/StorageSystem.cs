namespace UniCore.Storage
{
    public delegate void VersionChanged(object data, int oldVersion, int newVersion);

    public static class StorageSystem
    {
        internal static VersionChanged s_OnVersionChanged;
        private static StoragePipeline s_pipeline;

        public static event VersionChanged OnVersionChanged { add => s_OnVersionChanged += value; remove => s_OnVersionChanged -= value; }

        public static void SetSettings(ISettings settings)
        {
            s_pipeline = new StoragePipeline(settings);
        }

        public static byte[] GetKey()
        {
            InitializationIfNeed();
            return s_pipeline.Key;
        }

        public static void Save<T>(string fileName, T data)
        {
            InitializationIfNeed();
            s_pipeline.Save(fileName, data);
        }

        public static T Load<T>(string fileName)
        {
            InitializationIfNeed();
            return s_pipeline.Load<T>(fileName);
        }

        private static void InitializationIfNeed()
        {
            if (s_pipeline != null) return;
            ISettings setting = SettingsProvider.Load();
            s_pipeline = new StoragePipeline(setting);
        }
    }
}