using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace WFCLevelGeneration
{
    [ExecuteInEditMode]
    public class ModulesManager : MonoBehaviour
    {
        public ModulesInfo modulesInfo;
        public OnFaceEvent onFaceSelectEvent;
        public OnFaceEvent onFaceDeselectEvent;
        public OnModuleVariantsEvent onModuleVariantsShowEvent;
        public OnModuleVariantsEvent onModuleVariantsHideEvent;

        private void Awake()
        {
            if (onFaceSelectEvent == null)
                onFaceSelectEvent = new OnFaceEvent();
            if (onFaceDeselectEvent == null)
                onFaceDeselectEvent = new OnFaceEvent();
            if (onModuleVariantsShowEvent == null)
                onModuleVariantsShowEvent = new OnModuleVariantsEvent();
            if (onModuleVariantsHideEvent == null)
                onModuleVariantsHideEvent = new OnModuleVariantsEvent();
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