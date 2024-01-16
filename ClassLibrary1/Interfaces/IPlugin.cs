using LibHac.Lr;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
