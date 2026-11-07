using System.Collections;
using System.Collections.Generic;
using RageRooster.Core;
using UnityEngine;

namespace DebugRR
{
    [DefaultExecutionOrder(ExecutionOrders.Gameplay + 1)]
    public class DebugTextOverlay : MonoBehaviour
    {
        //public bool clearAtBeginningOfUpdate = true;
        public static TMPro.TMP_Text Text;

        public static bool Visible { get; private set; }

        //Coroutine coroutine;

        private void Awake() => Init();
        [ContextMenu("Init")]
        public void Init()
        {
            if (!Gameplay.Active) return;
            Text = GetComponent<TMPro.TMP_Text>();
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
            Visible = value;
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
