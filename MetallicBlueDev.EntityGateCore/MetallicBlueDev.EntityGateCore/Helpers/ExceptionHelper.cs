using System;
using System.Data.SqlClient;

namespace MetallicBlueDev.EntityGate.Helpers
{
    internal class ExceptionHelper
    {
        /// <summary>
        /// Determines if the request is poorly worded.
        /// </summary>
        /// <param name="ex">Exception</param>
        /// <returns></returns>
        internal static bool IsInvalidQuery(Exception ex)
        {
            var rslt = false;

            if (ex != null && ex is SqlException)
            {
                switch (((SqlException)ex).Number)
                {
                    case 102:
                    case 107:
                    case 170:
                    case 207:
                    case 208:
                    case 242:
                    case 547:
                    case 2705:
                    case 2812:
                    case 3621:
                    case 8152:
                    {
                        rslt = true;
                        break;
                    }
                }
            }

            return rslt;
        }
    }
}
