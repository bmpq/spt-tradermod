using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tarkin.tradermod.shared
{
    public class EyeBlinker : MonoBehaviour
    {
        public SkinnedMeshRenderer skinnedMeshRenderer;

        public string[] blendShapeNames = new string[] { "ARKIT_Blendshapes.eyeBlinkLeft", "ARKIT_Blendshapes.eyeBlinkRight" };

        public float minInterval = 2.0f;
        public float maxInterval = 5.0f;
        public float blinkDuration = 0.15f;

        private List<int> _blendShapeIndices = new List<int>();
        private Coroutine _blinkCoroutine;

        private void Start()
        {
            if (skinnedMeshRenderer == null)
            {
                skinnedMeshRenderer = GetComponent<SkinnedMeshRenderer>();
            }

            if (skinnedMeshRenderer == null)
            {
                Debug.LogError("EyeBlinker: No SkinnedMeshRenderer assigned or found!");
                enabled = false;
                return;
            }

            CacheBlendShapeIndices();

            _blinkCoroutine = StartCoroutine(BlinkRoutine());
        }

        private void CacheBlendShapeIndices()
        {
            _blendShapeIndices.Clear();
            Mesh mesh = skinnedMeshRenderer.sharedMesh;

            foreach (string shapeName in blendShapeNames)
            {
                int index = mesh.GetBlendShapeIndex(shapeName);
                if (index != -1)
                {
                    _blendShapeIndices.Add(index);
                }
                else
                {
                    Debug.LogWarning($"EyeBlinker: Blend shape '{shapeName}' not found on {skinnedMeshRenderer.name}.");
                }
            }
        }

        private IEnumerator BlinkRoutine()
        {
            while (true)
            {
                float waitTime = Random.Range(minInterval, maxInterval);
                yield return new WaitForSeconds(waitTime);

                yield return StartCoroutine(PerformBlink());
            }
        }

        private IEnumerator PerformBlink()
        {
            float timer = 0f;
            float halfDuration = blinkDuration / 2f;

            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                float weight = Mathf.Lerp(0f, 100f, timer / halfDuration);
                SetBlendShapes(weight);
                yield return null;
            }

            SetBlendShapes(100f);

            timer = 0f;
            while (timer < halfDuration)
            {
                timer += Time.deltaTime;
                float weight = Mathf.Lerp(100f, 0f, timer / halfDuration);
                SetBlendShapes(weight);
                yield return null;
            }

            SetBlendShapes(0f);
        }

        private void SetBlendShapes(float weight)
        {
            foreach (int index in _blendShapeIndices)
            {
                skinnedMeshRenderer.SetBlendShapeWeight(index, weight);
            }
        }

        private void OnDisable()
        {
            if (_blinkCoroutine != null)
            {
                StopCoroutine(_blinkCoroutine);
            }
            if (skinnedMeshRenderer != null && _blendShapeIndices.Count > 0)
            {
                SetBlendShapes(0f);
            }
        }
    }
}
