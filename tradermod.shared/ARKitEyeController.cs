using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace tarkin.tradermod.shared
{
    [ExecuteAlways]
    public class ARKitEyeController : MonoBehaviour
    {
        public SkinnedMeshRenderer targetMesh;

        [Space(10)]
        public Transform leftEyeOrigin;
        public Transform rightEyeOrigin;

        [Space(10)]
        public Transform lookTarget;

        [Range(10f, 90f)]
        public float maxEyeAngle = 40f;

        private int _idxDownLeft, _idxDownRight;
        private int _idxInLeft, _idxInRight;
        private int _idxOutLeft, _idxOutRight;
        private int _idxUpLeft, _idxUpRight;

        private void Start()
        {
            if (targetMesh == null) targetMesh = GetComponent<SkinnedMeshRenderer>();

            if (leftEyeOrigin == null) leftEyeOrigin = transform;
            if (rightEyeOrigin == null) rightEyeOrigin = transform;

            CacheBlendShapeIndices();
        }

        private void LateUpdate()
        {
            if (lookTarget == null || targetMesh == null) 
                return;

            UpdateSingleEye(leftEyeOrigin, true);
            UpdateSingleEye(rightEyeOrigin, false);
        }

        private void UpdateSingleEye(Transform eyeOrigin, bool isLeftEye)
        {
            Vector3 localTarget = eyeOrigin.InverseTransformPoint(lookTarget.position);

            float pitchAngle = Mathf.Atan2(localTarget.y, localTarget.z) * Mathf.Rad2Deg;
            float yawAngle = Mathf.Atan2(localTarget.x, localTarget.z) * Mathf.Rad2Deg;

            float xNorm = Mathf.Clamp(yawAngle / maxEyeAngle, -1f, 1f);
            float yNorm = Mathf.Clamp(pitchAngle / maxEyeAngle, -1f, 1f);

            float wUp = 0f, wDown = 0f;
            float wIn = 0f, wOut = 0f;

            if (yNorm > 0) wUp = yNorm * 100f;
            else wDown = -yNorm * 100f;

            if (isLeftEye)
            {
                if (xNorm > 0) wIn = xNorm * 100f;
                else wOut = -xNorm * 100f;

                SetWeight(_idxUpLeft, wUp);
                SetWeight(_idxDownLeft, wDown);
                SetWeight(_idxInLeft, wIn);
                SetWeight(_idxOutLeft, wOut);
            }
            else
            {
                if (xNorm > 0) wOut = xNorm * 100f;
                else wIn = -xNorm * 100f;

                SetWeight(_idxUpRight, wUp);
                SetWeight(_idxDownRight, wDown);
                SetWeight(_idxInRight, wIn);
                SetWeight(_idxOutRight, wOut);
            }
        }

        private void SetWeight(int index, float weight)
        {
            if (index != -1) targetMesh.SetBlendShapeWeight(index, weight);
        }

        private void CacheBlendShapeIndices()
        {
            Mesh m = targetMesh.sharedMesh;
            _idxDownLeft = m.GetBlendShapeIndex("ARKIT_Blendshapes.eyeLookDownLeft");
            _idxUpLeft = m.GetBlendShapeIndex("ARKIT_Blendshapes.eyeLookUpLeft");
            _idxInLeft = m.GetBlendShapeIndex("ARKIT_Blendshapes.eyeLookInLeft");
            _idxOutLeft = m.GetBlendShapeIndex("ARKIT_Blendshapes.eyeLookOutLeft");

            _idxDownRight = m.GetBlendShapeIndex("ARKIT_Blendshapes.eyeLookDownRight");
            _idxUpRight = m.GetBlendShapeIndex("ARKIT_Blendshapes.eyeLookUpRight");
            _idxInRight = m.GetBlendShapeIndex("ARKIT_Blendshapes.eyeLookInRight");
            _idxOutRight = m.GetBlendShapeIndex("ARKIT_Blendshapes.eyeLookOutRight");
        }
    }
}
