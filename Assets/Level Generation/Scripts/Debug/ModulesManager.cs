using System;
using UnityEngine;
using UnityEngine.Events;

namespace LevelGeneration
{
    [ExecuteInEditMode]
    public class ModulesManager : MonoBehaviour
    {
        public OnFaceEvent OnFaceSelectEvent;
        public OnFaceEvent OnFaceDeselectEvent;
        public OnModuleVariantsEvent OnModuleVariantsShowEvent;
        public OnModuleVariantsEvent OnModuleVariantsHideEvent;

        private void Awake()
        {
            if (OnFaceSelectEvent == null)
                OnFaceSelectEvent = new OnFaceEvent();
            if (OnFaceDeselectEvent == null)
                OnFaceDeselectEvent = new OnFaceEvent();
            if (OnModuleVariantsShowEvent == null)
                OnModuleVariantsShowEvent = new OnModuleVariantsEvent();
            if (OnModuleVariantsHideEvent == null)
                OnModuleVariantsHideEvent = new OnModuleVariantsEvent();
        }

        [Serializable]
        public class OnFaceEvent : UnityEvent<ModuleVisualizer.ModuleFace>
        {
        }

        [Serializable]
        public class OnModuleVariantsEvent : UnityEvent
        {
        }
    }
}