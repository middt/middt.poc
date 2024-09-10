using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapr.Actors.Runtime;

namespace dapr.actor.server.actors
{
    public class BankAccountActor : Actor, IBankAccountActor
    {
        private const string BalanceStateName = "statestore";

        public BankAccountActor(ActorHost host) : base(host)
        {
        }

        public async Task<decimal> GetBalanceAsync()
        {
            return await StateManager.GetStateAsync<decimal>(BalanceStateName);
        }

        public async Task DepositAsync(decimal amount)
        {
            var balance = await GetBalanceAsync();
            balance += amount;
            await StateManager.SetStateAsync(BalanceStateName, balance);
        }

        public async Task WithdrawAsync(decimal amount)
        {
            var balance = await GetBalanceAsync();
            if (balance < amount)
            {
                throw new System.Exception("Insufficient funds");
            }
            balance -= amount;
            await StateManager.SetStateAsync(BalanceStateName, balance);
        }

        protected override async Task OnActivateAsync()
        {
            if (!await StateManager.ContainsStateAsync(BalanceStateName))
            {
                await StateManager.SetStateAsync(BalanceStateName, 0m);
            }
        }
    }
}