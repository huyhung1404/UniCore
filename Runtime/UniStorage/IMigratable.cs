namespace UniCore.Storage
{
    public interface IMigratable
    {
        public void OnMigrate(int oldVersion, int newVersion);
    }
}