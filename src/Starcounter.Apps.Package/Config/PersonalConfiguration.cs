using System.IO;
using System.Xml.Serialization;

namespace Starcounter.Apps.Package.Config {
    [XmlRoot(ElementName = "service")]
    public class PersonalConfiguration {

        [System.Xml.Serialization.XmlElementAttribute("server-dir")]
        public string ServerDir { get; set; }

        internal static PersonalConfiguration Deserialize(Stream stream) {
            PersonalConfiguration config = new PersonalConfiguration();
            XmlSerializer ser = new XmlSerializer(config.GetType());
            return ser.Deserialize(stream) as PersonalConfiguration;
        }
    }
}
