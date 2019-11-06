using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Xml;
using MetallicBlueDev.EntityGate.Extensions;
using MetallicBlueDev.EntityGate.GateException;
using MetallicBlueDev.EntityGateCore.Properties;

namespace MetallicBlueDev.EntityGate.Configuration
{
    /// <summary>
    /// Configuration management.
    /// </summary>
    internal class EntityGateCoreConfigLoader
    {
        private const string ChildNodeName = "EntityGateCoreConfig";

        private static readonly object locker = new object();

        private static EntityGateCoreConfig[] configs = null;

        /// <summary>
        /// Create and load all configurations.
        /// </summary>
        /// <param name="section">Configuration section.</param>
        internal static void Initialize(XmlNode section)
        {
            lock (locker)
            {
                try
                {
                    configs = LoadConfigs(section);
                }
                catch (Exception ex)
                {
                    throw new ConfigurationEntityGateCoreException(Resources.InvalidConfiguration, ex);
                }
            }
        }

        /// <summary>
        /// Determines whether the configuration was initialized.
        /// </summary>
        /// <returns></returns>
        internal static bool Initialized()
        {
            lock (locker)
            {
                return configs != null;
            }
        }

        /// <summary>
        /// Returns the possible configurations.
        /// </summary>
        /// <returns></returns>
        internal static EntityGateCoreConfig[] GetConfigs()
        {
            lock (locker)
            {
                return configs;
            }
        }

        /// <summary>
        /// Returns the default configuration.
        /// </summary>
        /// <returns></returns>
        internal static EntityGateCoreConfig GetFirstConfig()
        {
            lock (locker)
            {
                EntityGateCoreConfig rslt = null;

                if (configs != null)
                {
                    rslt = configs.FirstOrDefault();
                }

                return rslt;
            }
        }

        /// <summary>
        /// Returns the requested configuration.
        /// </summary>
        /// <param name="connectionName">Name of the connection.</param>
        /// <returns></returns>
        internal static EntityGateCoreConfig GetConfig(string connectionName)
        {
            lock (locker)
            {
                EntityGateCoreConfig rslt = null;

                if (configs != null)
                {
                    rslt = configs.FirstOrDefault(conf => conf.ConnectionName.EqualsIgnoreCase(connectionName));
                }

                return rslt;
            }
        }

        /// <summary>
        /// Returns the connection via his name.
        /// </summary>
        /// <param name="connectionName">Name of the connection.</param>
        /// <returns></returns>
        internal static string GetConnectionString(string connectionName)
        {
            string result = null;

            try
            {
                if (ConfigurationManager.ConnectionStrings.Count > 0)
                {
                    var config = ConfigurationManager.ConnectionStrings[connectionName];

                    if (config != null)
                    {
                        result = config.ConnectionString;
                    }
                }
            }
            catch (Exception ex)
            {
                throw new ConfigurationEntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.ConnectionStringIsInvalid, connectionName), ex);
            }

            if (string.IsNullOrEmpty(result))
            {
                throw new ConfigurationEntityGateCoreException(string.Format(CultureInfo.InvariantCulture, Resources.ConnectionStringNotFound, connectionName));
            }

            return result;
        }

        /// <summary>
        /// Load all configurations.
        /// </summary>
        /// <param name="section">Configuration section.</param>
        /// <returns></returns>
        private static EntityGateCoreConfig[] LoadConfigs(XmlNode section)
        {
            var fullConfigs = new List<EntityGateCoreConfig>();
            var configProperties = GetEntityGateConfigProperties();

            foreach (var childNode in section.ChildNodes.Cast<XmlNode>().Where(node => node.Name.EqualsIgnoreCase(ChildNodeName)))
            {
                var config = LoadConfig(childNode, configProperties);

                fullConfigs.Add(config);
            }

            return fullConfigs.ToArray();
        }

        /// <summary>
        /// Returns the columns of the configuration.
        /// </summary>
        /// <returns></returns>
        private static PropertyInfo[] GetEntityGateConfigProperties()
        {
            return typeof(EntityGateCoreConfig).GetProperties();
        }

        /// <summary>
        /// Load the configuration.
        /// </summary>
        /// <param name="childNode">Node containing the configuration.</param>
        /// <param name="configProperties">The list of properties to look for.</param>
        /// <returns></returns>
        private static EntityGateCoreConfig LoadConfig(XmlNode childNode, PropertyInfo[] configProperties)
        {
            var config = new EntityGateCoreConfig();

            foreach (var prop in configProperties)
            {
                var value = default(string);
                var colNode = childNode.SelectSingleNode(prop.Name);

                if (colNode != null)
                {
                    value = colNode.InnerText;
                }
                else
                {
                    var xmlNode = childNode.Attributes.GetNamedItem(prop.Name);
                    value = xmlNode?.InnerText;
                }

                prop.SetValue(config, value, null);
            }

            return config;
        }
    }
}

