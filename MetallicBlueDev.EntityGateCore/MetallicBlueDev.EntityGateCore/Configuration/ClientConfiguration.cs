using System;
using System.Globalization;
using MetallicBlueDev.EntityGate.Extensions;
using MetallicBlueDev.EntityGate.GateException;
using MetallicBlueDev.EntityGate.Helpers;
using MetallicBlueDev.EntityGateCore.Properties;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;

namespace MetallicBlueDev.EntityGate.Configuration
{
    /// <summary>
    /// Client configuration.
    /// </summary>
    [Serializable()]
    public sealed class ClientConfiguration
    {
        private int maximumNumberOfAttempts = 0;
        private int attemptDelay = 0;
        private string connectionString = null;
        private int timeout = 0;

        /// <summary>
        /// Default value of LazyLoading.
        /// </summary>
        public bool LazyLoading { get; internal set; } = true;

        /// <summary>
        /// Determines whether the configuration has been updated.
        /// </summary>
        public bool Changed { get; private set; } = false;

        /// <summary>
        /// Maximum number of attempts.
        /// </summary>
        public int MaximumNumberOfAttempts
        {
            get => maximumNumberOfAttempts;
            set
            {
                if (value > 0)
                {
                    maximumNumberOfAttempts = value;
                    ConfigurationChanged();
                }
            }
        }

        /// <summary>
        /// Waiting time (in milliseconds) before the next attempt.
        /// </summary>
        public int AttemptDelay
        {
            get => attemptDelay;
            set
            {
                if (value > 0)
                {
                    attemptDelay = value;
                    ConfigurationChanged();
                }
            }
        }

        /// <summary>
        /// The raw connection string.
        /// </summary>
        public string ConnectionString
        {
            get => connectionString;
            set
            {
                if (value.IsNotNullOrEmpty())
                {
                    connectionString = value;
                    ConfigurationChanged();
                }
            }
        }

        /// <summary>
        /// Maximum time (in seconds) to run a query.
        /// </summary>
        public int Timeout
        {
            get => timeout;
            set
            {
                if (value > 3)
                {
                    timeout = value;
                    ConfigurationChanged();
                }
            }
        }

        /// <summary>
        /// Get or set the status of the notification.
        /// </summary>
        public bool CanUseNotification { get; set; }

        /// <summary>
        /// Determines if the backup of the original values is performed automatically.
        ///
        /// If the main entity implements the <see cref="InterfacedObject.IEntityObjectArchival"/> interface, the backup is automatically enabled.
        /// </summary>
        public bool AutomaticCheckOfOriginalValues { get; set; }

        /// <summary>
        /// Returns the logging system.
        /// Use <see cref="SetLoggerFactory(ILoggerFactory)(string)"/> to define it.
        /// </summary>
        public ILogger Logger { get; private set; } = null;

        /// <summary>
        /// Determines whether it is possible to use the event log.
        /// The <see cref="Logger"/> method must be defined.
        /// </summary>
        public bool CanUseLogging => Logger != null;

        /// <summary>
        /// Returns the <see cref="IDbContextOptionsExtension"/> used for configuration.
        /// Use <see cref="SetContextOptionsExtension(string)"/> to define it.
        /// </summary>
        public IDbContextOptionsExtension ContextOptionsExtension { get; private set; } = null;

        /// <summary>
        /// The system provider of logging.
        /// Use <see cref="SetLoggerFactory(ILoggerFactory)(string)"/> to define it.
        /// </summary>
        internal ILoggerFactory GateLoggerFactory { get; private set; } = null;

        /// <summary>
        /// New configuration.
        /// </summary>
        internal ClientConfiguration()
        {
            var defaultConfig = EntityGateCoreConfigLoader.GetFirstConfig();

            if (defaultConfig != null)
            {
                ChangeConnectionString(defaultConfig.ConnectionName);
            }
        }

        /// <summary>
        /// Configuration of the logging system.
        /// </summary>
        /// <param name="loggerFactory">The system provider of logging.</param>
        public void SetLoggerFactory(ILoggerFactory loggerFactory)
        {
            GateLoggerFactory = loggerFactory;

            if (GateLoggerFactory != null)
            {
                Logger = GateLoggerFactory.CreateLogger("EntityGateCore");
            }
        }

        /// <summary>
        /// Create the context option extension with the full name of the <see cref="IDbContextOptionsExtension"/> type.
        /// </summary>
        /// <param name="fullTypeName">Full name of the <see cref="IDbContextOptionsExtension"/> type.</param>
        public void SetContextOptionsExtension(string fullTypeName)
        {
            var optionExType = ReflectionHelper.MakeType(fullTypeName);
            SetContextOptionsExtension(optionExType);
        }

        /// <summary>
        /// Creating the context option extension with the requested <see cref="IDbContextOptionsExtension"/> type.
        /// </summary>
        /// <param name="contextOptionType"><see cref="IDbContextOptionsExtension"/> type.</param>
        public void SetContextOptionsExtension(Type contextOptionType)
        {
            ContextOptionsExtension = ReflectionHelper.MakeInstance<IDbContextOptionsExtension>(contextOptionType);
        }

        /// <summary>
        /// Change the connection string.
        /// </summary>
        /// <param name="connectionName">Name of the connection.</param>
        public void ChangeConnectionString(string connectionName)
        {
            var currentConfig = EntityGateCoreConfigLoader.GetConfig(connectionName);

            if (currentConfig != null)
            {
                CopyConfiguration(currentConfig);

                if (CanUseLogging && Logger.IsEnabled(LogLevel.Information))
                {
                    Logger.LogInformation(string.Format(CultureInfo.InvariantCulture, Resources.ConfigurationHasBeenLoaded, connectionName));
                }
            }
            else if (CanUseLogging && Logger.IsEnabled(LogLevel.Error))
            {
                Logger.LogError(string.Format(CultureInfo.InvariantCulture, Resources.UnableToFindConnectionString, connectionName));
            }
        }

        /// <summary>
        /// Update the connection configuration.
        /// </summary>
        /// <param name="optionsBuilder"><see cref="IDbContextOptionsExtension"/> builder.</param>
        internal void Update(DbContextOptionsBuilder optionsBuilder)
        {
            if (CanUseLogging)
            {
                optionsBuilder.UseLoggerFactory(GateLoggerFactory);
            }

            if (ContextOptionsExtension == null)
            {
                throw new ConfigurationEntityGateCoreException(Resources.ContextOptionsExtensionUndefined);
            }

            if (ContextOptionsExtension is RelationalOptionsExtension optionExt)
            {
                optionExt.WithConnectionString(ConnectionString);
                optionExt.WithCommandTimeout(Timeout);
            }

            optionsBuilder.Options.WithExtension(ContextOptionsExtension);

            ConfigurationSynchronized();
        }

        /// <summary>
        /// Signal that the configuration has changed.
        /// </summary>
        private void ConfigurationChanged()
        {
            if (!Changed)
            {
                Changed = true;

                if (CanUseLogging && Logger.IsEnabled(LogLevel.Information))
                {
                    Logger.LogInformation(Resources.ConfigurationChanged);
                }
            }
        }

        /// <summary>
        /// Copy of the configuration.
        /// </summary>
        /// <param name="config"></param>
        private void CopyConfiguration(EntityGateCoreConfig config)
        {
            ConnectionString = EntityGateCoreConfigLoader.GetConnectionString(config.ConnectionName);

            if (config.ContextOptionsExtension.IsNotNullOrEmpty())
            {
                SetContextOptionsExtension(config.ContextOptionsExtension);
            }

            MaximumNumberOfAttempts = config.MaximumNumberOfAttempts;
            AttemptDelay = config.AttemptDelay;
            LazyLoading = config.LazyLoading;
            Timeout = config.Timeout;
            AutomaticCheckOfOriginalValues = config.AutomaticCheckOfOriginalValues;
        }

        /// <summary>
        /// Indicates that the context is synchronized to this configuration.
        /// </summary>
        private void ConfigurationSynchronized()
        {
            if (Changed)
            {
                Changed = false;

                if (CanUseLogging && Logger.IsEnabled(LogLevel.Information))
                {
                    Logger.LogInformation(Resources.ConfigurationSynchronized);
                }
            }
        }
    }
}

