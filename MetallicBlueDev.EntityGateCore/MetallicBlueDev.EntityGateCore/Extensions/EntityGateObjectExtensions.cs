using System;
using System.Data.SqlClient;
using System.Globalization;
using MetallicBlueDev.EntityGate.Helpers;
using MetallicBlueDev.EntityGateCore.Properties;
using Microsoft.Extensions.Logging;

namespace MetallicBlueDev.EntityGate.Extensions
{
    internal static class EntityGateObjectExtensions
    {
        /// <summary>
        /// Marks the errors of the SQL exception.
        /// </summary>
        /// <param name="gate">The instance of EntityGate.</param>
        /// <param name="ex">The internal error.</param>
        internal static void LogException(this IEntityGateObject gate, Exception ex)
        {
            if (gate.Configuration.CanUseLogging && gate.Configuration.Logger.IsEnabled(LogLevel.Warning))
            {
                var lastQuery = gate.Token.SqlStatement.IsNotNullOrEmpty() ? gate.Token.SqlStatement : Resources.UnknownQuery;

                gate.Configuration.Logger.LogWarning(string.Format(CultureInfo.InvariantCulture, Resources.UnableToExecuteRequest, lastQuery));

                if (gate.Configuration.Logger.IsEnabled(LogLevel.Error))
                {
                    var sqlEx = ReflectionHelper.GetSqlServerException(ex);

                    if (sqlEx != null)
                    {
                        foreach (SqlError currentError in sqlEx.Errors)
                        {
                            gate.Configuration.Logger.LogError(currentError.ToString());
                        }
                    }
                    else
                    {
                        gate.Configuration.Logger.LogError(ex, ex.Message);
                    }
                }
            }
        }
    }
}
