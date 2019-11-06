using System.Data.Common;
using MetallicBlueDev.EntityGate;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MetallicBlueDev.EntityGateCore.Core
{
    internal class HintCommandInterceptor : DbCommandInterceptor
    {
        private readonly IEntityGateObject gate;

        internal HintCommandInterceptor(IEntityGateObject gate) : base()
        {
            this.gate = gate;
        }

        public override InterceptionResult<DbDataReader> ReaderExecuting(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
        {
            gate.Token.SqlStatement = command.CommandText;
            return result;
        }
    }
}
