using UnityEngine;
using ToolbarControl_NS;
using KSP.Localization;

namespace AntennaHelper
{
    [KSPAddon(KSPAddon.Startup.MainMenu, true)]
    public class RegisterToolbar : MonoBehaviour
    {
        void Start()
        {
            ToolbarControl.RegisterMod(AHEditor.MODID, Localizer.Format(AHEditor.MODNAME));
        }
    }
}