using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using RomManagerShared.Base;

namespace RomManagerShared.Utils
{
    public static class PluginManager
    {
        private static readonly List<IPlugin> Plugins = [];
        public static void LoadPlugins(string pluginDirectory)
        {
            var dllextensions = new string[] { "dll" }.ToList();
            // Load the assembly dynamically
            var pluginDlls = FileUtils.GetFilesInDirectoryWithExtensions(pluginDirectory, dllextensions);
            foreach (var dll in pluginDlls)
            {
                Assembly pluginAssembly = Assembly.LoadFrom(dll);
                Type pluginType = pluginAssembly.GetType("RomManagerPlugin.PluginMain");
                if (pluginType is IPlugin)
                {
                    Plugins.Add((IPlugin)pluginType);
                }
            }
            //// Find the plugin's entry point (assuming it has a class with a method named 'Run')
            //Type pluginType = pluginAssembly.GetType("Plugin.PluginClass");
            //

            //// Create an instance of the plugin class
            //object pluginInstance = Activator.CreateInstance(pluginType);

            //// Invoke the 'Run' method
            //methodInfo.Invoke(pluginInstance, null);
        }

        //public static void InitializePlugins()
        //{
        //    foreach (var plugin in Plugins)
        //    {
        //        // Configure plugins based on requirements
        //        plugin.RegisterAppConfigs("configPath");
        //        var requiredInterfaces = plugin.GetRequiredInterfaces();
        //        plugin.RegisterServices(requiredInterfaces);
        //    }
        //}
    }
}
