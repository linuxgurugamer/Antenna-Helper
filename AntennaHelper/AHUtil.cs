using System;
using System.Collections.Generic;
using UnityEngine;

namespace AntennaHelper
{
	[KSPAddon (KSPAddon.Startup.SpaceCentre, true)]
	public class AHUtil : MonoBehaviour
	{
		public static Texture toolbarButtonTexSatOn, toolbarButtonTexDishOn, toolbarButtonTexOff;
		public static Texture signalPerDistanceTex;

		// MapView textures :
		public static Texture circleGreenTex, circleYellowTex, circleOrangeTex, circleRedTex;

		public static List<MyTuple> signalPlanetList;

		public static CelestialBody homePlanet;

		public static Vector2 centerScreen;

		public void Start ()
		{
			DontDestroyOnLoad (this);
			centerScreen = new Vector2 (Screen.width / 2f, Screen.height / 2f);

			// load textures :
			circleGreenTex = (Texture)GameDatabase.Instance.GetTexture ("AntennaHelper/Textures/circle_green_a_1024", false);
			circleYellowTex = (Texture)GameDatabase.Instance.GetTexture ("AntennaHelper/Textures/circle_yellow_a_1024", false);
			circleOrangeTex = (Texture)GameDatabase.Instance.GetTexture ("AntennaHelper/Textures/circle_orange_a_1024", false);
			circleRedTex = (Texture)GameDatabase.Instance.GetTexture ("AntennaHelper/Textures/circle_red_a_1024", false);

			toolbarButtonTexSatOn = (Texture)GameDatabase.Instance.GetTexture ("AntennaHelper/Textures/icon_sat_on", false);
			toolbarButtonTexDishOn = (Texture)GameDatabase.Instance.GetTexture ("AntennaHelper/Textures/icon_dish_on", false);
			toolbarButtonTexOff = (Texture)GameDatabase.Instance.GetTexture ("AntennaHelper/Textures/icon_off", false);
			signalPerDistanceTex = (Texture)GameDatabase.Instance.GetTexture ("AntennaHelper/Textures/signal_per_distance", false);

			homePlanet = FlightGlobals.GetHomeBody ();

			signalPlanetList = new List<MyTuple> ();
			foreach (CelestialBody moon in homePlanet.orbitingBodies) {
				signalPlanetList.Add (new MyTuple (moon.bodyName, moon.orbit.PeR, moon.orbit.ApR));
			}
			foreach (CelestialBody planet in FlightGlobals.Bodies[0].orbitingBodies) {
				if (planet != homePlanet) {
					signalPlanetList.Add (GetDistancePlanet (homePlanet, planet));
				}
			}
		}

//		public void OnDestroy ()
//		{
//			
//		}

		public static void DummyVoid () {}// For the toolbar button

		#region Math
		public static double TruePower (double power) {
			// return the "true power" of the antenna, stock power * range modifier
			return power * HighLogic.CurrentGame.Parameters.CustomParams<CommNet.CommNetParams> ().rangeModifier;
		}

		public static double GetAWCE (List<ModuleDataTransmitter> antennas, bool applyMod = true)
		{
			// Get the Average Weighted Combinability Exponent for this set of antennas
			// From the wiki : SUM (( Antenna 'n' Power * Antenna 'n' Exponent ) : ( Antenna 'n+1' Power * Antenna 'n+1' Exponent )) / SUM ( Antenna 'n' Power ) : ( Antenna 'n+1' Power )
			// (( 100e9 * 0.75 ) + ( 500e3 * 1.00 )) / ( 100e9 + 500e3 ) = 0.75000125
			// x / y = z

			double x = 0;
			double y = 0;
			double z;

			if (antennas.Count == 1) {
				return antennas [0].antennaCombinableExponent;
			}

			foreach (ModuleDataTransmitter ant in antennas) {
				if (applyMod) {
					x += TruePower (ant.antennaPower) * ant.antennaCombinableExponent;
					y += TruePower (ant.antennaPower);
				} else {
					x += ant.antennaPower * ant.antennaCombinableExponent;
					y += ant.antennaPower;
				}
			}
			z = x / y;
			return z;
		}

		public static double GetVesselPower (List<ModuleDataTransmitter> antennas, bool applyMod = true)
		{
			// Get the total antenna power for the vessel, the list in parameter need to be carefully selected, 
			// remove not combinable, relay and/or direct

			double strongestAnt = 0;
			double allAnt = 0;
			double awce = GetAWCE (antennas, applyMod);
			foreach (ModuleDataTransmitter ant in antennas) {
				allAnt += TruePower (ant.antennaPower);
				if (TruePower (ant.antennaPower) > strongestAnt) {
					strongestAnt = TruePower (ant.antennaPower);
				}
			}
			double vesselPower = strongestAnt * Math.Pow (allAnt / strongestAnt, awce);
			return vesselPower;
		}

		public static double GetRange (double activeAntPower, double targetAntPower)
		{
			return Math.Sqrt (activeAntPower * targetAntPower);
		}

		public static MyTuple GetDistancePlanet (CelestialBody home, CelestialBody target)
		{
			double max = home.orbit.ApR + target.orbit.ApR;
			double min;
			if (home.orbit.PeR > target.orbit.PeR) {
				min = home.orbit.PeR - target.orbit.PeR;
			} else {
				min = target.orbit.PeR - home.orbit.PeR;
			}
			return new MyTuple (target.bodyName, min, max);
		}

		public static double GetSignalStrength (double maxRange, double distance)
		{
			if (distance > maxRange) { return 0; }

			double relativeDistance = 1 - (distance / maxRange);//Math.Abs (1 - (distance / maxRange));
//			Debug.Log ("Relative distance = " + relativeDistance);
			double strength = (3 - (2 * relativeDistance)) * (relativeDistance * relativeDistance);

			if (strength < 0) { return 0; }

			return strength;
		}

		public static double GetREALSignal (CommNet.CommLink link, bool aIsRelay = true)
		{
			Vessel vesselA = link.a.transform.GetComponent<Vessel> ();
			double powerA = GetActualVesselPower (vesselA, aIsRelay);
			double powerB;
			Vector3 positionB;
			if (link.b.isHome) {
				positionB = link.b.position;
				powerB = GameVariables.Instance.GetDSNRange 
					(ScenarioUpgradeableFacilities.GetFacilityLevel 
						(SpaceCenterFacility.TrackingStation));
			} else {
				Vessel vesselB = link.b.transform.GetComponent<Vessel> ();
				powerB = GetActualVesselPower (vesselB, true);
				positionB = vesselB.GetTransform ().position;
			}

			double maxRange = GetRange (powerA, powerB);

			double distance = Vector3.Distance (vesselA.GetTransform ().position, positionB);

			return GetSignalStrength (maxRange, distance);
		}

		public static double GetREALSignal (CommNet.CommPath path)
		{
			double signal = 1d;

			bool first = true;

			foreach (CommNet.CommLink link in path) {
				signal *= GetREALSignal (link, !first);
				first = false;
			}
			return signal;
		}

		public static double GetRealSignal (CommNet.CommPath path, Vessel v = null)
		{
			// return the signal strength between a vessel and the dsn when there are relays between them
//			Debug.Log ("[AH] getting real signal for " + path[0].a.name);
			if (v == null) {
				v = FlightGlobals.ActiveVessel;
			}
			double signal = 1d;
			foreach (CommNet.CommLink link in path) {
//				Debug.Log ("[AH] link : " + link.ToString ());
				if (link.a.transform.GetComponent<Vessel> () != v) {
					signal *= link.signalStrength;
				}
			}
			return signal;
		}

		public static double GetRealSignalForTrackingStation (CommNet.CommPath path)
		{
			// return the signal strength between a vessel and the dsn when there are relays between them
//			Debug.Log ("[AH] getting real signal for " + path[0].a.name);

			double signal = 1d;
			foreach (CommNet.CommLink link in path) {
//				Debug.Log ("[AH] link : " + link.ToString ());
				signal *= link.signalStrength;
			}
			return signal;
		}

		public static double GetActualVesselPower (Vessel v, bool onlyRelay = false, bool checkIfExtended = true, bool applyMod = true)
		{
			// This function should be more generic and also used in the editor
			// will see later...
			double biggest = 0;
			List<ModuleDataTransmitter> combList = new List<ModuleDataTransmitter> ();

			if (v.parts.Count > 0) {
				foreach (Part p in v.parts) {
					if (p.Modules.Contains<ModuleDataTransmitter> ()) {
						// Check extendability
						if (checkIfExtended) {
							if (p.Modules.Contains<ModuleDeployableAntenna>()) {
								ModuleDeployableAntenna antDep = p.Modules.GetModule<ModuleDeployableAntenna> ();
								if ((antDep.deployState != ModuleDeployablePart.DeployState.EXTENDED) 
									&& (antDep.deployState != ModuleDeployablePart.DeployState.EXTENDING)) {
									continue;
								}
							}
						}

						ModuleDataTransmitter ant = p.Modules.GetModule<ModuleDataTransmitter> ();

						// Check if relay
						if (onlyRelay) {
							if (ant.antennaType != AntennaType.RELAY) {
								continue;
							}
						}

						// All good
						if (ant.antennaPower > biggest) {
							biggest = ant.antennaPower;
						}
						if (ant.antennaCombinable) {
							combList.Add (ant);
						}
					}
				}
			} else {
				// This is for the tracking station, as the active vessel isn't actually active
				foreach (ProtoPartSnapshot p in v.protoVessel.protoPartSnapshots) {
					
					if (p.partPrefab.Modules.Contains<ModuleDataTransmitter> ()) {
						// Check extendability
						if (checkIfExtended) {
							if (p.partPrefab.Modules.Contains<ModuleDeployableAntenna>()) {
								string antDep = p.modules.Find (x => x.moduleName == "ModuleDeployableAntenna").moduleValues.GetValue ("deployState");
								if ((antDep != "EXTENDED") 
								    && (antDep != "EXTENDING")) {
									continue;
								}
							}
						}

						ModuleDataTransmitter ant = p.partPrefab.Modules.GetModule<ModuleDataTransmitter> ();
						// Check if relay
						if (onlyRelay) {
							if (ant.antennaType != AntennaType.RELAY) {
								continue;
							}
						}

						// All good
						if (ant.antennaPower > biggest) {
							biggest = ant.antennaPower;
						}
						if (ant.antennaCombinable) {
							combList.Add (ant);
						}
					}
				}
			}

			double comb;
			if (applyMod) {
				biggest = TruePower (biggest);
				comb = GetVesselPower (combList);
			} else {
				comb = GetVesselPower (combList, false);
			}


			if (comb > biggest) {
				return comb;
			} else {
				return biggest;
			}
		}

		public static double GetDistanceAt100 (double maxRange)
		{
			return maxRange / 77.1241569002155d;
		}

		public static double GetDistanceAt75 (double maxRange)
		{
			return maxRange / 3.060623967191712d;
		}

		public static double GetDistanceAt50 (double maxRange)
		{
			return maxRange / 1.998667554768621d;
		}

		public static double GetDistanceAt25 (double maxRange)
		{
			return maxRange / 1.483619335214967d;
		}

		public static double GetDistanceAt0 (double maxRange)
		{
			return maxRange / 1.013136434488117d;
		}

		public static double GetDistanceFor (double sS, double maxRange)
		{
			int newSS = (int)Math.Truncate (sS * 100d);
			return GetDistanceFor (newSS, maxRange);
		}

		public static double GetDistanceFor (int sS, double maxRange)
		{
			if (sS < 0) { sS = 0; }
			double dist;
			switch (sS) {
			case 0://0.6
				dist = maxRange / 1.047584719414499d;
				break;
			case 1:
				dist = maxRange / 1.062582782629115d;
				break;
			case 2:
				dist = maxRange / 1.091742601042011d;
				break;
			case 3:
				dist = maxRange / 1.115636532280093d;
					break;
			case 4:
				dist = maxRange / 1.136881804217293d;
					break;
			case 5:
				dist = maxRange / 1.156539171888104d;
					break;
			case 6:
				dist = maxRange / 1.175114349153859d;
					break;
			case 7:
				dist = maxRange / 1.192913846438235d;
					break;
			case 8:
				dist = maxRange / 1.206241638748131d;
					break;
			case 9:
				dist = maxRange / 1.226975913232661d;
					break;
			case 10:
				dist = maxRange / 1.243470663744964d;
					break;
			case 11:
				dist = maxRange / 1.259730368945333d;
					break;
			case 12:
				dist = maxRange / 1.275806578169752d;
					break;
			case 13:
				dist = maxRange / 1.291758601383292d;
					break;
			case 14:
				dist = maxRange / 1.307635208199639d;
					break;
			case 15:
				dist = maxRange / 1.323453676909387d;
					break;
			case 16:
				dist = maxRange / 1.339253428483652d;
					break;
			case 17:
				dist = maxRange / 1.355078621631047d;
					break;
			case 18:
				dist = maxRange / 1.370917540988991d;
					break;
			case 19:
				dist = maxRange / 1.386820077901072d;
					break;
			case 20:
				dist = maxRange / 1.402798862196235d;
				break;
			case 21:
				dist = maxRange / 1.418867995463065d;
				break;
			case 22:
				dist = maxRange / 1.435065319133585d;
				break;
			case 23:
				dist = maxRange / 1.451375558323068d;
				break;
			case 24:
				dist = maxRange / 1.467828538111668d;
				break;
			case 25:
				dist = maxRange / 1.484408895648559d;
				break;
			case 26:
				dist = maxRange / 1.501247657713d;
				break;
			case 27:
				dist = maxRange / 1.518236239211017d;
				break;
			case 28:
				dist = maxRange / 1.535435813964251d;
				break;
			case 29:
				dist = maxRange / 1.552847588503511d;
				break;
			case 30:
				dist = maxRange / 1.570499259413742d;
				break;
			case 31:
				dist = maxRange / 1.588393660939914d;
				break;
			case 32:
				dist = maxRange / 1.606561427456803d;
				break;
			case 33:
				dist = maxRange / 1.62500727343559d;
				break;
			case 34:
				dist = maxRange / 1.643750547060143d;
				break;
			case 35:
				dist = maxRange / 1.66281202561854d;
				break;
			case 36:
				dist = maxRange / 1.682214011810481d;
				break;
			case 37:
				dist = maxRange / 1.701949216230781d;
				break;
			case 38:
				dist = maxRange / 1.722057073010135d;
				break;
			case 39:
				dist = maxRange / 1.742547548414234d;
				break;
			case 40:
				dist = maxRange / 1.763464478536904d;
				break;
			case 41:
				dist = maxRange / 1.784786655598696d;
				break;
			case 42:
				dist = maxRange / 1.806560399410912d;
				break;
			case 43:
				dist = maxRange / 1.828817890188019d;
				break;
			case 44:
				dist = maxRange / 1.851556754706981d;
				break;
			case 45:
				dist = maxRange / 1.874830303761782d;
				break;
			case 46:
				dist = maxRange / 1.898638099017631d;
				break;
			case 47:
				dist = maxRange / 1.923038393801196d;
				break;
			case 48:
				dist = maxRange / 1.948033107987443d;
				break;
			case 49:
				dist = maxRange / 1.973686116546957d;
				break;
			case 50:
				dist = maxRange / 2.000002215089314d;
				break;
			case 51:
				dist = maxRange / 2.027029567308258d;
				break;
			case 52:
				dist = maxRange / 2.054797403130631d;
				break;
			case 53:
				dist = maxRange / 2.08338336087284d;
				break;
			case 54:
				dist = maxRange / 2.11277590470151d;
				break;
			case 55:
				dist = maxRange / 2.143083916640055d;
				break;
			case 56:
				dist = maxRange / 2.174325085575795d;
				break;
			case 57:
				dist = maxRange / 2.206543055092116d;
				break;
			case 58:
				dist = maxRange / 2.23983830882057d;
				break;
			case 59:
				dist = maxRange / 2.2742373846654d;
				break;
			case 60:
				dist = maxRange / 2.309824542545699d;
				break;
			case 61:
				dist = maxRange / 2.346691525050539d;
				break;
			case 62:
				dist = maxRange / 2.384938374070643d;
				break;
			case 63:
				dist = maxRange / 2.42461099354079d;
				break;
			case 64:
				dist = maxRange / 2.465822420565277d;
				break;
			case 65:
				dist = maxRange / 2.508696431987994d;
				break;
			case 66:
				dist = maxRange / 2.55336882125139d;
				break;
			case 67:
				dist = maxRange / 2.599988867129489d;
				break;
			case 68:
				dist = maxRange / 2.648645409294202d;
				break;
			case 69:
				dist = maxRange / 2.699550514348217d;
				break;
			case 70:
				dist = maxRange / 2.75285904724928d;
				break;
			case 71:
				dist = maxRange / 2.80882552722106d;
				break;
			case 72:
				dist = maxRange / 2.867646598397255d;
				break;
			case 73:
				dist = maxRange / 2.929585156983327d;
				break;
			case 74:
				dist = maxRange / 2.995031726218621d;
				break;
			case 75:
				dist = maxRange / 3.064177519283549d;
				break;
			case 76:
				dist = maxRange / 3.13754617171186d;
				break;
			case 77:
				dist = maxRange / 3.215461493851417d;
				break;
			case 78:
				dist = maxRange / 3.298517483870725d;
				break;
			case 79:
				dist = maxRange / 3.387337851702284d;
				break;
			case 80:
				dist = maxRange / 3.482642094348437d;
				break;
			case 81:
				dist = maxRange / 3.58519545354361d;
				break;
			case 82:
				dist = maxRange / 3.696032140207482d;
				break;
			case 83:
				dist = maxRange / 3.816293819471329d;
				break;
			case 84:
				dist = maxRange / 3.947582186300789d;
				break;
			case 85:
				dist = maxRange / 4.091651351827997d;
				break;
			case 86:
				dist = maxRange / 4.25062323611051d;
				break;
			case 87:
				dist = maxRange / 4.427512778054214d;
				break;
			case 88:
				dist = maxRange / 4.625753029085959d;
				break;
			case 89:
				dist = maxRange / 4.85017266579611d;
				break;
			case 90:
				dist = maxRange / 5.107299903735095d;
				break;
			case 91:
				dist = maxRange / 5.405786199239483d;
				break;
			case 92:
				dist = maxRange / 5.75843229789262d;
				break;
			case 93:
				dist = maxRange / 6.183785876726211d;
				break;
			case 94:
				dist = maxRange / 6.710604177312694d;
				break;
			case 95:
				dist = maxRange / 7.38823785548682d;
				break;
			case 96:
				dist = maxRange / 8.305649306297941d;
				break;
			case 97:
				dist = maxRange / 9.648892641845866d;
				break;
			case 98:
				dist = maxRange / 11.89869215741285d;
				break;
			case 99:
				dist = maxRange / 16.97604807892919d;
				break;
			case 100:// 99.5
			default:
				dist = maxRange / 24.15992259149688d;
				break;
			}
			return dist;
		}

		public static float GetMapScale (double distance)
		{
			return (float)distance / 2949.852507374631f;
		}

		public static string FormatCircleSelect (GUICircleSelection circleSelect)
		{
			string circleSelectClean;
			switch (circleSelect) {
			case GUICircleSelection.ACTIVE:
				circleSelectClean = "#autoLOC_AH_0045";
				break;
			case GUICircleSelection.DSN:
				circleSelectClean = "#autoLOC_AH_0046";
				break;
			case GUICircleSelection.DSN_AND_RELAY:
				circleSelectClean = "#autoLOC_AH_0047";
				break;
			case GUICircleSelection.RELAY:
				circleSelectClean = "#autoLOC_AH_0048";
				break;
			case GUICircleSelection.NONE:
			default:
				circleSelectClean = "#autoLOC_AH_0049";
				break;
			}
			return circleSelectClean;
		}
		#endregion
	}

	public class MyTuple
	{
		public string item1 = null;
		public double item2 = Double.NaN;
		public double item3 = Double.NaN;

		public MyTuple (string itemStr, double itemDouble)
		{
			item1 = itemStr;
			item2 = itemDouble;
		}

		public MyTuple (string itemStr, double itemDouble, double itemDouble2)
		{
			item1 = itemStr;
			item2 = itemDouble;
			item3 = itemDouble2;
		}
	}

	public enum GUICircleSelection
	{
		ACTIVE,
		DSN,
		RELAY,
		DSN_AND_RELAY,
		NONE
	}
}

