using System.Collections;
using System.Collections.Generic;
using EditorAttributes;
using UnityEditor;
using UnityEditor.Recorder;
using UnityEngine;
using UnityEngine.UIElements;

public class AnimationBakeUtility : MonoBehaviour
{
    [Button]
    public void DO()
    {
        if (!TryGetComponent(out Animator animator)) return;
        RecorderWindow recorder = EditorWindow.GetWindow<RecorderWindow>();
        if (recorder == null) return;
        

        if (TryGetComponent(out PlayerMovementBody movementBody)) movementBody.enabled = false;
        if (TryGetComponent(out PlayerController controller)) controller.enabled = false;
        if (TryGetComponent(out PlayerStateMachine stateMachine)) stateMachine.enabled = false;

        animator.SetFloat("CurrentSpeed", RunSpeedValue);

        for (int i = 0; i < LayerApplications.Count; i++)
            animator.SetLayerWeight(i, LayerApplications[i]);

        DoAll().Begin(this);
        IEnumerator DoAll()
        {
            for (int i = 0; i < StateName.Count; i++)
            {
                yield return DoAnimation(StateName[i]);
                yield return null;
                yield return null;
            }
        }
        IEnumerator DoAnimation(string name)
        {
            animator.Play(name);

            float clipLength = -25f;
            for (int i = -1; i < LayerApplications.Count; i++)
            {
                float iLength = animator.GetCurrentAnimatorStateInfo(i).length;
                if (iLength > clipLength) clipLength = iLength;
            }

            yield return null;
            recorder.StartRecording();
            yield return new WaitForSeconds(clipLength);
            recorder.StopRecording();
        }
    }
    public List<string> StateName;
    public List<float> LayerApplications;
    public float RunSpeedValue;
}
