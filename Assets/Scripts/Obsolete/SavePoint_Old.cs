using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace RageRooster.Obsolete
{
    public class SavePoint_Old : MonoBehaviour, IInteractable
    {
        public Transform SpawnPoint;
        public UltEvents.UltEvent onSpawnEvent;

        bool canInteract = true;
        bool IInteractable.canInteract => canInteract;

        public void Checkpoint()
        {
            Gameplay.spawnSceneName = gameObject.scene.name;
            Gameplay.spawnPointID = GetID();
        }

        public void Save()
        {
        }
#if UNITY_EDITOR
        [Button("Play from here.")]
        public void BeginFromHere()
        {
            EditorState.LoadFromSavePointID = GetID();
            UnityEditor.EditorApplication.isPlaying = true;
        }
#endif

        public int GetID()
        {
            Zones.ZoneRoot Root = GetComponentInParent<Zones.ZoneRoot>();
            int ID = -1;
            for (int i = 0; i < Root.spawns.Length; i++)
                if (Root.spawns[i] == this)
                {
                    ID = i;
                    break;
                }
            return ID;
        }

        bool IInteractable.Interaction()
        {
            new CoroutinePlus(Save_CR(), this);
            IEnumerator Save_CR()
            {
                Save();
                //PlayerHealth.Global.HealToFull();

                Light light = gameObject.GetOrAddComponent<Light>();
                light.type = LightType.Point;
                light.intensity = 10;
                light.color = Color.green;
                while (light.intensity > 0)
                {
                    yield return null;
                    light.intensity -= 0.1f;
                }
            }
            return true;
        }

        public Vector3 PopupPosition => transform.position + Vector3.up * 2;
    }
}