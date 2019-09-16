using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace AntennaHelper
{
	public class AntennaHelperSettings : GameParameters.CustomParameterNode
	{
		public override string Title {
			get {
				return /*"Antenna Helper"*/Localizer.Format ("#autoLOC_AH_0001");
			}
		}
		public override string Section {
			get {
				return /*"Antenna Helper"*/Localizer.Format ("#autoLOC_AH_0001");
			}
		}
		public override string DisplaySection {
			get {
				return /*"Antenna Helper"*/Localizer.Format ("#autoLOC_AH_0001");
			}
		}
		public override int SectionOrder {
			get {
				return 1;
			}
		}
		public override GameParameters.GameMode GameMode {
			get {
				return GameParameters.GameMode.ANY;
			}
		}
		public override bool HasPresets {
			get {
				return false;
			}
		}

		[GameParameters.CustomParameterUI (/*"Enable in the Editor"*/"#autoLOC_AH_0065", toolTip = "#autoLOC_AH_0072")]
		public bool enableInEditor = true;

		[GameParameters.CustomParameterUI (/*"Enable in the Tracking Station"*/"#autoLOC_AH_0066", toolTip = "#autoLOC_AH_0072")]
		public bool enableInTrackingStation = true;

		[GameParameters.CustomParameterUI (/*"Enable in Flight"*/"#autoLOC_AH_0067", toolTip = "#autoLOC_AH_0072")]
		public bool enableInFlight = true;

		[GameParameters.CustomParameterUI (/*"Enable in the MapView"*/"#autoLOC_AH_0068", toolTip = "#autoLOC_AH_0072")]
		public bool enableInMapView = true;

		[GameParameters.CustomStringParameterUI (/*Flight and MapView*/"#autoLOC_AH_0069", autoPersistance = false)]
		public string paramDummy1 = "";

		[GameParameters.CustomFloatParameterUI (/*Second(s) Between GUI Update*/"#autoLOC_AH_0070", displayFormat = "0.0", maxValue = 2f, minValue = 0, toolTip = "#autoLOC_AH_0072")]
		public double delayFlightUI = .5;

		[GameParameters.CustomStringParameterUI (/*If set to 0, GUI is updated only when opening the window*/"#autoLOC_AH_0071", autoPersistance = false)]
		public string paramDummy = "";

		[GameParameters.CustomFloatParameterUI (/*debug*/"Start Delay", displayFormat = "0.0", maxValue = 10f, minValue = .1f)]
		public float startDelay = 1f;

		public override bool Enabled (MemberInfo member, GameParameters parameters)
		{
			return true;
		}

		public override bool Interactible (MemberInfo member, GameParameters parameters)
		{
			if (member.Name == "delayFlightUI" || member.Name == "paramDummy1" || member.Name == "paramDummy") {
				if (!enableInFlight && !enableInMapView) {
					return false;
				}
			}
			return true;
		}
	}
}

