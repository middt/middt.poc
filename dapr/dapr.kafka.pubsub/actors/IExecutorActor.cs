using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapr.Actors;


public interface IExecutorActor : IActor
{
    Task<bool> ExecuteAndAddRecordAsync(Func<Task<bool>> action);
}
