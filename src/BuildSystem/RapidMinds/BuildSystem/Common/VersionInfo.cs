using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using System.Globalization;

namespace RapidMinds.BuildSystem.Common
{
    [Serializable]
    public class VersionInfo
    {
        #region Properties
        public string Configuration { get; set; }
        public string Platform { get; set; }
        public string Version { get; set; }
        public string IDFullBase32 { get; set; }
        /// <summary>
        /// Gets or sets the ID tail base64.
        /// </summary>
        /// <remarks>
        /// This is also used as the foldername of one instance
        /// </remarks>
        /// <value>
        /// The ID tail base64.
        /// </value>
        public string IDTailBase64 { get; set; }
        public string IDTailDecimal { get; set; }

        public string BuildDate
        {
            get
            {
                return String.Format("{0:yyyy-MM-dd HH:mm:dd}", this.VersionCreation);
            }
            set
            {
                if (value == null)
                {
                    this.VersionCreation = DateTime.MinValue;
                }
                else
                {
                    this.VersionCreation = DateTime.ParseExact(value, "yyyy-MM-dd HH:mm:dd", CultureInfo.CurrentCulture);
                }
            }
        }


        [XmlIgnoreAttribute]
        public string FileName { get; set; }

        [XmlIgnoreAttribute]
        public bool IsLocked
        {
            get
            {
                if (!string.IsNullOrEmpty(this.FileName))
                {
                    return string.Equals(".lock", Path.GetExtension(this.FileName), StringComparison.CurrentCultureIgnoreCase);
                }

                return false;
            }
        }

        [XmlIgnoreAttribute]
        public DateTime VersionCreation { get; set; }


        #endregion


        public static VersionInfo Load(string file)
        {
            VersionInfo versionInfo;
            if (File.Exists(file))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(VersionInfo));
                TextReader tr = new StreamReader(file);
                versionInfo = (VersionInfo)serializer.Deserialize(tr);
                tr.Close();

                versionInfo.FileName = file;
                // TODO: Validate the VersionInfo content


            }
            else
            {
                throw new FileNotFoundException("Can not load version info", file);
            }
            return versionInfo;
        }

        public static void Save(VersionInfo versionInfo, string file)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(VersionInfo));
            TextWriter tw = new StreamWriter(file);
            serializer.Serialize(tw, versionInfo);
            tw.Close();

            versionInfo.FileName = file;

        }

        public static void LockInstance(VersionInfo versionInfo)
        {
            string newName = Path.ChangeExtension(versionInfo.FileName, "lock");
            File.Move(versionInfo.FileName, newName);
            versionInfo.FileName = newName;
        }

        public static void UnLockInstance(VersionInfo versionInfo)
        {
            string newName = Path.ChangeExtension(versionInfo.FileName, "xml");
            File.Move(versionInfo.FileName, newName);
            versionInfo.FileName = newName;
        }


        /// <summary>
        /// Verifies the specified version info is complet,including binaries.
        /// </summary>
        /// <param name="versionInfo">The version info.</param>
        /// <returns></returns>
        public static bool Verify(VersionInfo versionInfo)
        {

            try
            {
                // Check file existance 
                if (!File.Exists(versionInfo.FileName)) throw new InvalidDataException("Corrupt Version info, invalid filepath");

                // Check directory existance
                string rootPath = Path.GetDirectoryName(versionInfo.FileName);
                if (!Directory.Exists(rootPath)) throw new InvalidDataException("Corrupt Version info, invalid path");

                // Check binary directory existance
                string instancePath = Path.Combine(rootPath, versionInfo.IDTailBase64);
                if (!Directory.Exists(instancePath)) throw new InvalidDataException("Corrupt Version info, invalid binary path");

                // Check binaries existance
                string[] files = Directory.GetFiles(instancePath);
                if (files.Length == 0) throw new InvalidDataException("Corrupt Version info, no binaries");

                return true;
            }
            catch (Exception)
            {
                return false;
            }




        }

    }




    //<?xml version="1.0" encoding="UTF-8" ?>
    //<VersionInfo>
    //  <Configuration>xxxxxx</Configuration>
    //  <Platform>yyyyyy</Platform>
    //  <Version>1.0.30.0</Version>
    //  <IDFullBase32>zzzzzzz</IDFullBase32>
    //  <IDTailBase64>634443403516299602</IDTailBase64>
    //  <IDTailDecimal>16299602</IDTailDecimal>
    //</VersionInfo>



}
