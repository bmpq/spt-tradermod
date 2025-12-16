using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace tarkin.tradermod.eft
{
    internal class TraderCameraController : IDisposable
    {
        private Camera _cam;
        private Coroutine _fadeCoroutine;

        public void Setup(Transform camPoint)
        {
            if (_cam == null)
            {
                _cam = GameObject.Instantiate(Resources.Load<GameObject>("Cam2_fps_hideout")).GetComponent<Camera>();
                _cam.GetComponent<PrismEffects>().useExposure = true;
                _cam.GetComponent<Cinemachine.CinemachineBrain>().enabled = false;
                _cam.fieldOfView = 60;
                GameObject.DontDestroyOnLoad(_cam.gameObject);
            }

            if (CameraClass.Instance.Camera != null)
                CameraClass.Instance.IsActive = false;

            _cam.gameObject.SetActive(true);
            _cam.transform.SetPositionAndRotation(camPoint.position, camPoint.rotation);
        }

        public void SetActive(bool active)
        {
            if (_cam != null) 
                _cam.gameObject.SetActive(active);
        }

        public Task FadeToBlack(bool toBlack)
        {
            var tcs = new TaskCompletionSource<bool>();

            if (_fadeCoroutine != null)
                CoroutineRunner.Instance.StopCoroutine(_fadeCoroutine);

            _fadeCoroutine = CoroutineRunner.Instance.StartCoroutine(FadeCoroutine(toBlack, tcs));

            return tcs.Task;
        }

        private IEnumerator FadeCoroutine(bool black, TaskCompletionSource<bool> completionSource)
        {
            if (_cam == null)
            {
                completionSource.TrySetResult(false);
                yield break;
            }

            float t = 0;
            var effectControl = _cam.GetComponent<CC_BrightnessContrastGamma>();
            effectControl.enabled = true;

            float targetBrightness = black ? -100f : 0;
            float startBrightness = effectControl.brightness;

            Quaternion startWorldRot = black
                ? _cam.transform.rotation
                : Quaternion.LookRotation(-_cam.transform.right + _cam.transform.forward * 2f, _cam.transform.up);
            Quaternion targetWorldRot = black
                ? Quaternion.LookRotation(_cam.transform.right + _cam.transform.forward * 2f, _cam.transform.up)
                : _cam.transform.rotation;

            while (t < 1f)
            {
                t += Time.deltaTime * 3f;
                t = Mathf.Clamp01(t);
                float easedT = black ? EaseInCubic(0, 1f, t) : EaseOutCubic(0, 1f, t);

                effectControl.brightness = Mathf.Lerp(startBrightness, targetBrightness, easedT);
                _cam.transform.rotation = Quaternion.Slerp(startWorldRot, targetWorldRot, easedT);

                yield return null;
            }

            completionSource.TrySetResult(true);
        }

        private static float EaseInCubic(float start, float end, float value)
        {
            end -= start;
            return end * value * value * value + start;
        }

        private static float EaseOutCubic(float start, float end, float value)
        {
            value--;
            end -= start;
            return end * (value * value * value + 1) + start;
        }

        public void Dispose()
        {
            if (_fadeCoroutine != null) 
                CoroutineRunner.Instance.StopCoroutine(_fadeCoroutine);

            if (_cam != null) 
                GameObject.Destroy(_cam.gameObject);
        }
    }
}