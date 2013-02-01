using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration.Install;
using System.Runtime.InteropServices;
using System.Collections;
using Starcounter;
using System.Xml;

namespace Starcounter.InstallerEngine
{
    class SettingsLoader
    {
        /// <summary>
        /// Reads all keys from a certain section in config.
        /// </summary>
        /// <param name="pathToConfig">Path to XML file.</param>
        /// <param name="sectionName">Name of the section to read from.</param>
        /// <param name="settingDict">Dictionary to which add settings.</param>
        internal static void LoadConfigFile(String pathToConfig, String sectionName, Dictionary<String, String> settingDict)
        {
            XmlReaderSettings readerSettings = new XmlReaderSettings();

            // NOTE: Ignoring all comments in config.
            readerSettings.IgnoreComments = true;

            XmlReader reader = XmlReader.Create(pathToConfig, readerSettings);

            XmlDocument xmlConfig = new XmlDocument();
            xmlConfig.Load(reader);

            XmlNodeList installNodeList = xmlConfig.GetElementsByTagName(sectionName);
            if (installNodeList.Count != 1)
            {
                throw ErrorCode.ToException(Error.SCERRINSTALLERCORRUPTEDSETUPFILE,
                    "Corrupted setup settings file " + pathToConfig);
            }

            XmlNode installNode = installNodeList.Item(0);
            foreach (XmlNode installSetting in installNode.ChildNodes)
                settingDict.Add(installSetting.Name, installSetting.InnerText);
        }
        
        /// <summary>
        /// Get value of a certain setting.
        /// </summary>
        /// <param name="settingName">Name of the setting.</param>
        /// <param name="settingsDict">Dictionary to search from.</param>
        /// <returns>Setting value.</returns>
        internal static String GetSettingValue(String settingName, Dictionary<String, String> settingsDict)
        {
            if (settingsDict == null)
            {
                throw ErrorCode.ToException(Error.SCERRINSTALLERINTERNALPROBLEM, "Settings are not yet initialized from setup settings file.");
            }

            String settingValue;
            if (settingsDict.TryGetValue(settingName, out settingValue))
            {
                return settingValue;
            }

            throw ErrorCode.ToException(Error.SCERRINSTALLERINTERNALPROBLEM, "Can't find setting " + settingName);
        }
    }
}
