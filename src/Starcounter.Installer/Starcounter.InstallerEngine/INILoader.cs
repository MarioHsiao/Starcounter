using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration.Install;
using System.Runtime.InteropServices;
using System.Collections;
using Starcounter;

namespace Starcounter.InstallerEngine
{
    class INILoader
    {
        [DllImport("KERNEL32.DLL", EntryPoint = "GetPrivateProfileStringW", SetLastError = true,
                   CharSet = CharSet.Unicode, ExactSpelling = true,
                   CallingConvention = CallingConvention.StdCall)]
        static extern int GetPrivateProfileString(String lpAppName, String lpKeyName, String lpDefault,
                                                  String lpReturnString, int nSize, String lpFilename);

        /// <summary>
        /// Reads all keys from a certain section of an INI file.
        /// </summary>
        /// <param name="pathToIni">Path to an INI file.</param>
        /// <param name="sectionName">Name of the section to read from.</param>
        /// <returns>Obtained list of keys.</returns>
        static List<String> IniReadKeys(String pathToIni, String sectionName)
        {
            String allSectionKeys = new String(' ', 32768);
            int errCode = GetPrivateProfileString(sectionName, null, null, allSectionKeys, 32768, pathToIni);
            if (errCode <= 0) return null; // Can't find needed section with keys.
            List<String> keysList = new List<String>(allSectionKeys.Split('\0'));
            keysList.RemoveRange(keysList.Count - 2, 2);
            return keysList;
        }
        
        /// <summary>
        /// Reads value for a certain key and section in INI file.
        /// </summary>
        /// <param name="pathToIni">Path to INI file.</param>
        /// <param name="sectionName">Name of the section to read from.</param>
        /// <param name="keyName">Name of the key which value should be obtain.</param>
        /// <returns>Value of the key.</returns>
        static String IniReadValue(String pathToIni, String sectionName, String keyName)
        {
            String keyValue = new String(' ', 4096);
            int errCode = GetPrivateProfileString(sectionName, keyName, null, keyValue, 4096, pathToIni);
            if (errCode <= 0) return null; // Can't find needed section with key value.
            String[] keySplit = keyValue.Split('\0');
            if (keySplit.Length != 2)
            {
                throw ErrorCode.ToException(Error.SCERRINSTALLERCANTREADSETTINGVALUE,
                    "Can't read value from INI file " + pathToIni + " for the following key: " + keyName);
            }
            return keySplit[0];
        }

        /// <summary>
        /// Reads all settings from specified INI file
        /// and saves their key-value pairs to dictionary.
        /// Needs initialization of dictionary first.
        /// </summary>
        internal static void LoadINIFile(String pathToIni, String iniSection, Dictionary<String, String> targetDict)
        {
            // Getting all key names from the section first.
            List<String> keysList = IniReadKeys(pathToIni, iniSection);
            if (keysList == null)
            {
                throw ErrorCode.ToException(Error.SCERRINSTALLERCORRUPTEDINIFILE,
                    "Can't read keys from INI settings file " + pathToIni + " in section: " + iniSection);
            }

            // Getting value for each key.
            foreach(String keyName in keysList)
            {
                String keyValue = IniReadValue(pathToIni, iniSection, keyName);
                if (keyValue == null)
                {
                    throw ErrorCode.ToException(Error.SCERRINSTALLERCANTREADSETTINGVALUE,
                        "Can't read value from INI file " + pathToIni + " for the following key: " + keyName);
                }
                targetDict.Add(keyName, keyValue);
            }
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
                throw ErrorCode.ToException(Error.SCERRINSTALLERINTERNALPROBLEM, "Settings are not yet initialized from INI file.");
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
