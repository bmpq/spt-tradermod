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
        public Transform headTransform;
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
            if (headTransform == null) headTransform = transform;

            _idxDownLeft = targetMesh.sharedMesh.GetBlendShapeIndex("ARKIT_Blendshapes.eyeLookDownLeft");
            _idxDownRight = targetMesh.sharedMesh.GetBlendShapeIndex("ARKIT_Blendshapes.eyeLookDownRight");
            _idxInLeft = targetMesh.sharedMesh.GetBlendShapeIndex("ARKIT_Blendshapes.eyeLookInLeft");
            _idxInRight = targetMesh.sharedMesh.GetBlendShapeIndex("ARKIT_Blendshapes.eyeLookInRight");
            _idxOutLeft = targetMesh.sharedMesh.GetBlendShapeIndex("ARKIT_Blendshapes.eyeLookOutLeft");
            _idxOutRight = targetMesh.sharedMesh.GetBlendShapeIndex("ARKIT_Blendshapes.eyeLookOutRight");
            _idxUpLeft = targetMesh.sharedMesh.GetBlendShapeIndex("ARKIT_Blendshapes.eyeLookUpLeft");
            _idxUpRight = targetMesh.sharedMesh.GetBlendShapeIndex("ARKIT_Blendshapes.eyeLookUpRight");
        }

        private void LateUpdate()
        {
            if (lookTarget == null || targetMesh == null || headTransform == null) 
                return;

            Vector3 localTargetPos = headTransform.InverseTransformPoint(lookTarget.position);

            float pitch = Mathf.Atan2(localTargetPos.y, localTargetPos.z) * Mathf.Rad2Deg;
            float yaw = Mathf.Atan2(localTargetPos.x, localTargetPos.z) * Mathf.Rad2Deg;

            float xNorm = Mathf.Clamp(yaw / maxEyeAngle, -1f, 1f);
            float yNorm = Mathf.Clamp(pitch / maxEyeAngle, -1f, 1f);

            UpdateEyeShapes(xNorm, yNorm);
        }

        private void UpdateEyeShapes(float x, float y)
        {
            float vUp = 0f, vDown = 0f;
            float hLeftIn = 0f, hLeftOut = 0f;
            float hRightIn = 0f, hRightOut = 0f;

            if (y > 0) vUp = y * 100f;
            else vDown = -y * 100f;

            if (x > 0) hLeftIn = x * 100f;
            else hLeftOut = -x * 100f;

            if (x > 0) hRightOut = x * 100f;
            else hRightIn = -x * 100f;

            if (_idxUpLeft != -1) targetMesh.SetBlendShapeWeight(_idxUpLeft, vUp);
            if (_idxUpRight != -1) targetMesh.SetBlendShapeWeight(_idxUpRight, vUp);

            if (_idxDownLeft != -1) targetMesh.SetBlendShapeWeight(_idxDownLeft, vDown);
            if (_idxDownRight != -1) targetMesh.SetBlendShapeWeight(_idxDownRight, vDown);

            if (_idxInLeft != -1) targetMesh.SetBlendShapeWeight(_idxInLeft, hLeftIn);
            if (_idxOutLeft != -1) targetMesh.SetBlendShapeWeight(_idxOutLeft, hLeftOut);

            if (_idxInRight != -1) targetMesh.SetBlendShapeWeight(_idxInRight, hRightIn);
            if (_idxOutRight != -1) targetMesh.SetBlendShapeWeight(_idxOutRight, hRightOut);
        }
    }
}
