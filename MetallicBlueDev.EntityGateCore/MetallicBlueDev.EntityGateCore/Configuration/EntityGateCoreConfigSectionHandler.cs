using System.Configuration;
using System.Xml;

namespace MetallicBlueDev.EntityGate.Configuration
{
    /// <summary>
    /// EntityGate configuration section.
    /// </summary>
    public class EntityGateCoreConfigSectionHandler : IConfigurationSectionHandler
    {
        /// <summary>
        /// Creating the EntityGate configuration.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="configContext"></param>
        /// <param name="section"></param>
        /// <returns></returns>
        public object Create(object parent, object configContext, XmlNode section)
        {
            if (!EntityGateCoreConfigLoader.Initialized())
            {
                EntityGateCoreConfigLoader.Initialize(section);
            }

            return EntityGateCoreConfigLoader.GetConfigs();
        }
    }
}

