using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace tarkin.tradermod.shared
{
    [ExecuteAlways]
    public class EyeTest : MonoBehaviour
    {
        [Range(0f, 1.1f)]
        [SerializeField] float magnitude = 1.0f;
        [Range(0f, 22f)]
        [SerializeField] float speed = 1.0f;

        float t;

        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
            float x = Mathf.Sin(Time.timeSinceLevelLoad * speed) * magnitude;
            float y = Mathf.Cos(Time.timeSinceLevelLoad * speed) * magnitude;

            transform.localPosition = new Vector3(x, y, 0);
        }
    }
}
