using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace tarkin.tradermod.shared
{
    public class OnEnableDisableUnityEvent : MonoBehaviour
    {
        [SerializeField] UnityEvent eventOnEnable;
        [SerializeField] UnityEvent eventOnDisable;

        private Coroutine _enableCoroutine;
        private bool _wasEnableInvoked = false;

        void OnEnable()
        {
            _wasEnableInvoked = false;
            _enableCoroutine = StartCoroutine(CallEnableAfterOneFrameDelay());
        }

        void OnDisable()
        {
            if (_enableCoroutine != null)
            {
                StopCoroutine(_enableCoroutine);
                _enableCoroutine = null;
            }

            if (_wasEnableInvoked)
            {
                eventOnDisable?.Invoke();
            }

            _wasEnableInvoked = false;
        }

        private IEnumerator CallEnableAfterOneFrameDelay()
        {
            yield return null;

            eventOnEnable?.Invoke();

            _wasEnableInvoked = true;
            _enableCoroutine = null;
        }
    }
}