using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapr.Actors;

namespace dapr.actor.server.actors
{
    public interface IBankAccountActor : IActor
    {
        Task<decimal> GetBalanceAsync();
        Task DepositAsync(decimal amount);
        Task WithdrawAsync(decimal amount);
    }
}