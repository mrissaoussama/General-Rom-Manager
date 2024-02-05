namespace RomManagerShared.Base
{
    public interface IPlugin
    {
        string PluginName { get; }
        public List<string> Options { get; set; }
        void Execute(int optionIndex);
        void RegisterAppConfigs(string configPath);
        //   List<IService> GetRequiredInterfaces();
        //   void RegisterServices(List<IService> services);
    }
}
