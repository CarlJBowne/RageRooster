using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewTarget : MonoBehaviour
{
    [SerializeReference] public Behaviour.Melee meleeBehavior;
    [SerializeReference] public Behaviour.Ranged rangedBehavior;
    [SerializeReference] public Behaviour.Grabbable grabbableBehavior;
    [SerializeReference] public Behaviour.Lassoable LassoableBehavior;
    [SerializeReference] public Behaviour.Interactable interactableBehavior;

    [System.Serializable]
    public abstract class Behaviour : PolymorphicObject
    {
        public Target This;
        public virtual void OnDeTargeted(Target nextTarget) { }
        public virtual void OnTargeted(Target prevTarget) { }

        [System.Serializable]
        public class Melee : Behaviour { }
        [System.Serializable]
        public class Ranged : Behaviour { }
        [System.Serializable]
        public class Grabbable : Behaviour { }
        [System.Serializable]
        public class Lassoable : Behaviour { }
        [System.Serializable]
        public class Interactable : Behaviour
        {
            public Vector3 PopupPosition;
            public override void OnTargeted(Target prevTarget)
            {
                TargetingManager.InteractionPopup.SetActive(true);
                TargetingManager.InteractionPopup.transform.position = This.transform.position + PopupPosition;
            }
            public override void OnDeTargeted(Target nextTarget)
            {
                if (!nextTarget) TargetingManager.InteractionPopup.SetActive(false);
            }
        }
    }
}

