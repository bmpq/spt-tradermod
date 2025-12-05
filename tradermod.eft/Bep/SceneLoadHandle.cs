using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace tarkin.tradermod.bep
{
    public class SceneLoadHandle
    {
        private readonly TaskCompletionSource<bool> _readyTcs = new TaskCompletionSource<bool>();
        private readonly TaskCompletionSource<Scene> _activationTcs = new TaskCompletionSource<Scene>();

        public Task WaitUntilReady() => _readyTcs.Task;

        public Task<Scene> Activate()
        {
            ShouldActivate = true;
            return _activationTcs.Task;
        }

        internal bool ShouldActivate { get; private set; } = false;

        internal void SetReady() => _readyTcs.TrySetResult(true);
        internal void SetResult(Scene scene) => _activationTcs.TrySetResult(scene); 
        internal void SetCanceled()
        {
            _readyTcs.TrySetCanceled();
            _activationTcs.TrySetCanceled();
        }
    }
}
