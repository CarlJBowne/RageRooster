using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DebugRR
{
    [DefaultExecutionOrder(ExecutionOrders.Gameplay-50)]
    public class DebugTextOverlay : MonoBehaviour
    {
        public TMPro.TMP_Text text;
        //public bool clearAtBeginningOfUpdate = true;
        public static TMPro.TMP_Text Text;

        //CoroutinePlus coroutine;

        private void Awake()
        {
            Text = text;
            Input.Debug.ToggleTextOverlay.performed += ctx =>
            {
                SetVisible(!Text.gameObject.activeSelf);
            };
            SetVisible(false);
        }

        //private void OnEnable()
        //{
        //    if(clearAtBeginningOfUpdate) coroutine = new(ProcessEnum(), this);
        //    ClearText();
        //}
        //private void OnDisable()
        //{
        //    if (clearAtBeginningOfUpdate) coroutine?.StopAuto();
        //}
        //
        //IEnumerator ProcessEnum()
        //{
        //    while (true)
        //    {
        //        yield return new WaitForEndOfFrame();
        //        ClearText();
        //    }
        //}

        public static void SetVisible(bool value)
        {
            if (Text == null) return;
            Text.gameObject.SetActive(value);
        }

        public static void SetText(string value)
        {
            if (Text == null) return;
            Text.text = value;
        }

        public static void AppendText(string value)
        {
            if (Text == null) return;
            Text.text += value;
        }

        public static void AppendNewLine(string value)
        {
            if (Text == null) return;
            Text.text += "\n" + value;
        }

        public static void ClearText()
        {
            if (Text == null) return;
            Text.text = string.Empty;
        }

    }
}
