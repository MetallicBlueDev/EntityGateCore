using System;

namespace MetallicBlueDev.EntityGate.Core
{
    /// <summary>
    /// Token containing the defining context.
    /// </summary>
    [Serializable]
    public sealed class EntityGateToken
    {
        /// <summary>
        /// Define or obtain the state of the backup of the original values.
        /// 
        /// <list type="bullet">
        /// <item>True: the backup is enabled.</item>
        /// <item>False: the backup is disabled.</item>
        /// </list> 
        /// </summary>
        public bool SaveOriginalValues { get; set; } = false;

        /// <summary>
        /// Determines whether entity tracking is enabled.
        /// </summary>
        public bool IsTracked { get; internal set; } = true;

        /// <summary>
        /// Determines whether the data recording is allowed.
        ///
        /// False to get the data from the base, True to update in the database.
        /// </summary>
        public bool AllowedSaving { get; internal set; } = false;

        /// <summary>
        /// Get the number of attempts to execute the query.
        /// </summary>
        public int NumberOfAttempts { get; internal set; } = 0;

        /// <summary>
        /// Get the number of rows affected by the last query.
        /// Returns <code>-1</code> on error.
        /// </summary>
        public int NumberOfRows { get; internal set; } = -1;

        /// <summary>
        /// Get or set the SQL query.
        /// </summary>
        public string SqlStatement { get; internal set; } = null;

        /// <summary>
        /// New token containing the context.
        /// </summary>
        internal EntityGateToken()
        {
        }
    }
}

