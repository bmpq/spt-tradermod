using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using UnityEngine;

namespace tarkin.tradermod.eft.Bep
{
    public static class AsyncExtensions
    {
        public static TaskAwaiter GetAwaiter(this AsyncOperation asyncOp)
        {
            var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (asyncOp.isDone)
            {
                tcs.SetResult(true);
            }
            else
            {
                asyncOp.completed += _ => tcs.TrySetResult(true);
            }

            return ((Task)tcs.Task).GetAwaiter();
        }
    }
}
