using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using KSP.Localization;

namespace AntennaHelper
{
    public static class AHShipList
    {
        private static Dictionary<string, Dictionary<string, string>> listEditorVessel = new Dictionary<string, Dictionary<string, string>>();
        private static Dictionary<string, Dictionary<string, string>> listFlyingVessel = new Dictionary<string, Dictionary<string, string>>();
        private static string savePath;
        private static string loadedGame;
        private static bool loadedOnce;
        public static bool shipListReady;

        // Part list
        public static List<ModuleDataTransmitter> listAntennaPart;

        static AHShipList()
        {
            savePath = RegisterToolbar.ApplicationRootPath + "GameData/AntennaHelper/PluginData/VesselList.cfg";
            loadedOnce = false;
            shipListReady = false;


        }

        private static void DoStart()
        {
            loadedGame = HighLogic.CurrentGame.Title;
            if (!LoadFromFile(loadedGame))
            {
                SaveToFile();
            }

            GetPartList();

            loadedOnce = true;
        }

        private static void GetPartList()
        {
            listAntennaPart = new List<ModuleDataTransmitter>();

            foreach (AvailablePart aPart in PartLoader.LoadedPartsList)
            {
                if (aPart != null)
                {
                    foreach (ModuleDataTransmitter antenna in aPart.partPrefab.FindModulesImplementing<ModuleDataTransmitter>())
                    {
                        if (antenna.antennaType != AntennaType.INTERNAL)
                        {
                            listAntennaPart.Add(antenna);
                        }
                    }
                }
            }
            listAntennaPart.Sort(CompareAntenna);
        }

        private static int CompareAntenna(ModuleDataTransmitter a, ModuleDataTransmitter b)
        {
            if (a == null)
            {
                if (b == null)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
            if (b == null)
            {
                if (a == null)
                {
                    return 0;
                }
                else
                {
                    return -1;
                }
            }

            if (a.antennaType == b.antennaType)
            {
                if (a.antennaPower == b.antennaPower)
                {
                    return 0;
                }
                if (a.antennaPower > b.antennaPower)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }

            if (a.antennaType == AntennaType.INTERNAL)
            {
                return 1;
            }
            if (a.antennaType == AntennaType.DIRECT)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }

        private static bool LoadFromFile(string saveTitle)
        {
            listEditorVessel = new Dictionary<string, Dictionary<string, string>>();

            ConfigNode confNode = ConfigNode.Load(savePath);
            if (confNode == null)
            {
                Debug.Log("[AH] no vessel list can be found");
                return false;
            }

            ConfigNode saveNode = confNode.GetNode("SAVE", "title", saveTitle);
            if (saveNode == null)
            {
                Debug.Log("[AH] no vessel list for this save");
                return false;
            }

            if (!saveNode.HasNode("VESSEL"))
            {
                Debug.Log("[AH] no vessel in the vessel list for this save");
                return true;
            }

            foreach (ConfigNode vesselNode in saveNode.GetNodes("VESSEL"))
            {
                string vesselPid = vesselNode.GetValue("pid");
                listEditorVessel.Add(vesselPid, new Dictionary<string, string>());
                listEditorVessel[vesselPid].Add("name", vesselNode.GetValue("name"));
                listEditorVessel[vesselPid].Add("type", vesselNode.GetValue("type"));
                listEditorVessel[vesselPid].Add("powerTotal", vesselNode.GetValue("powerTotal"));
                listEditorVessel[vesselPid].Add("powerRelay", vesselNode.GetValue("powerRelay"));
                listEditorVessel[vesselPid].Add("saveDate", vesselNode.GetValue("saveDate"));
                listEditorVessel[vesselPid].Add("connectedTo", "");
                listEditorVessel[vesselPid].Add("realSignal", "0");
            }
            return true;
        }

        private static void SaveToFile()
        {
            ConfigNode mainNode = ConfigNode.Load(savePath);
            if (mainNode == null)
            {
                mainNode = new ConfigNode("AH_VESSEL_LIST");
            }

            ConfigNode saveNode = mainNode.GetNode("SAVE", "title", loadedGame);
            if (saveNode == null)
            {
                saveNode = mainNode.AddNode("SAVE");
                saveNode.AddValue("title", loadedGame);
            }

            foreach (KeyValuePair<string, Dictionary<string, string>> vesselPairInfo in listEditorVessel)
            {

                ConfigNode vesselNode = saveNode.GetNode("VESSEL", "pid", vesselPairInfo.Key);
                if (vesselNode == null)
                {
                    vesselNode = saveNode.AddNode("VESSEL");
                    vesselNode.AddValue("pid", vesselPairInfo.Key);
                }

                foreach (KeyValuePair<string, string> kvp in vesselPairInfo.Value)
                {
                    vesselNode.SetValue(kvp.Key, kvp.Value, true);
                }
            }

            // Delete un-wanted vessel
            foreach (ConfigNode vNode in saveNode.GetNodes("VESSEL"))
            {
                if (!listEditorVessel.ContainsKey(vNode.GetValue("pid")))
                {
                    saveNode.RemoveNode(vNode);
                }
            }

            mainNode.Save(savePath);
        }

        public static List<Vessel> GetAllFlyingVessel()
        {
            return FlightGlobals.Vessels.FindAll(
                v => (v.vesselType != VesselType.EVA) &&
                (v.vesselType != VesselType.Flag) &&
                (v.vesselType != VesselType.SpaceObject) &&
                (v.vesselType != VesselType.Unknown) &&
                (v.vesselType != VesselType.Debris));
        }

        public static List<Vessel> GetFlyingRelays()
        {
            List<Vessel> flyingRelays = new List<Vessel>();
            foreach (Vessel v in GetAllFlyingVessel())
            {
                if (AHUtil.GetActualVesselPower(v, true) > 0)
                {
                    flyingRelays.Add(v);
                }
            }
            return flyingRelays;
        }

        public static Dictionary<Vessel, Dictionary<Vessel, LinkPath>> GetFlyingVessel(List<Vessel> masterList = null)
        {
            if (masterList == null)
            {
                masterList = FlightGlobals.Vessels.FindAll(
                    v => (v.vesselType != VesselType.EVA) &&
                    (v.vesselType != VesselType.Flag) &&
                    (v.vesselType != VesselType.SpaceObject) &&
                    (v.vesselType != VesselType.Unknown) &&
                    (v.vesselType != VesselType.Debris));
            }

            List<Vessel> relays = masterList.FindAll(v =>
               (v.vesselType == VesselType.Relay) && (v.Connection.IsConnected));

            Dictionary<Vessel, Dictionary<Vessel, LinkPath>> returnDict = new Dictionary<Vessel, Dictionary<Vessel, LinkPath>>();

            foreach (Vessel vessel in masterList)
            {
                returnDict.Add(vessel, new Dictionary<Vessel, LinkPath>());
                var allRelays = relays.FindAll(v => v != vessel);
                for (int r = 0; r < allRelays.Count; r++)
                {
                    var relay = allRelays[r];
                    //foreach (Vessel relay in relays.FindAll (v => (v != vessel)))
                    //{
                    returnDict[vessel].Add(relay, new LinkPath(relay));
                }
            }
            return returnDict;
        }

        public static void ParseFlyingVessel(bool doRealSignalNow = false)
        {
            listFlyingVessel = new Dictionary<string, Dictionary<string, string>>();
            for (int i =0; i < FlightGlobals.Vessels.Count ; i++)
            {
                var v = FlightGlobals.Vessels[i];
                //foreach (Vessel v in FlightGlobals.Vessels) {
                if ((v.vesselType != VesselType.EVA) &&
                    (v.vesselType != VesselType.Flag) &&
                    (v.vesselType != VesselType.SpaceObject) &&
                    (v.vesselType != VesselType.Unknown) &&
                    (v.vesselType != VesselType.Debris)/* && 
					(v.parts.Count > 0)*/)
                {

                    string pid = v.id.ToString();
                    double vesselPower = AHUtil.GetActualVesselPower(v, false, true, false);
                    double vesselRelayPower = AHUtil.GetActualVesselPower(v, true, true, false);


                    listFlyingVessel.Add(pid, new Dictionary<string, string>());
                    listFlyingVessel[pid].Add("name", v.GetName());
                    listFlyingVessel[pid].Add("type", v.vesselType.ToString());
                    listFlyingVessel[pid].Add("powerTotal", vesselPower.ToString());
                    listFlyingVessel[pid].Add("powerRelay", vesselRelayPower.ToString());
                    listFlyingVessel[pid].Add("saveDate", "");
                    listFlyingVessel[pid].Add("realSignal", "");
                    listFlyingVessel[pid].Add("connectedTo", "");

                    if (doRealSignalNow)
                    {
                        ComputeRealSignal(v);
                    }
                }
            }
        }

        public static void ComputeRealSignal(Vessel v)
        {
            //			yield return new WaitForSeconds (1f);
            //			Debug.Log ("[AH] Computing real signal for vessel : " + v.GetName ());
            if (listFlyingVessel.ContainsKey(v.id.ToString()))
            {
                //				Debug.Log ("[AH] " + v.GetName () + " is in the list");
                listFlyingVessel[v.id.ToString()]["connectedTo"] = "";
                if (v.Connection.IsConnected)
                {
                    listFlyingVessel[v.id.ToString()]["realSignal"] = AHUtil.GetRealSignalForTrackingStation(v.Connection.ControlPath).ToString();
                    if (!v.Connection.ControlPath[0].b.isHome)
                    {
                        listFlyingVessel[v.id.ToString()]["connectedTo"] = v.Connection.ControlPath[0].b.transform.GetComponent<Vessel>().id.ToString();
                    }
                }
                else
                {
                    listFlyingVessel[v.id.ToString()]["realSignal"] = "0";
                }

                //				Debug.Log ("[AH] its real signal = " + listFlyingVessel [v.id.ToString ()] ["realSignal"]);
            }
            else
            {
                //				Debug.Log ("[AH] " + v.GetName () + " is not in the list");
            }
            //			Debug.Log ("[AH] (Re-)Computing signal for vessel : " + v.GetName () + " : " + listFlyingVessel [v.id.ToString ()] ["realSignal"]);
        }

        public static void ComputeAllSignal()
        {
            Debug.Log("[AH] Computing real signal for all vessels");
            foreach (Vessel v in FlightGlobals.Vessels)
            {
                if (listFlyingVessel.ContainsKey(v.id.ToString()))
                {

                    listFlyingVessel[v.id.ToString()]["connectedTo"] = "";
                    listFlyingVessel[v.id.ToString()]["realSignal"] = "0";

                    if (v.Connection.IsConnected)
                    {
                        listFlyingVessel[v.id.ToString()]["realSignal"] = AHUtil.GetRealSignalForTrackingStation(v.Connection.ControlPath).ToString();
                        if (!v.Connection.ControlPath[0].b.isHome)
                        {
                            listFlyingVessel[v.id.ToString()]["connectedTo"] = v.Connection.ControlPath[0].b.transform.GetComponent<Vessel>().id.ToString();
                        }
                    }
                    Debug.Log("[AH] " + v.GetName() + " is connected to " + listFlyingVessel[v.id.ToString()]["connectedTo"] + " with a signal of " + listFlyingVessel[v.id.ToString()]["realSignal"]);
                }
            }
        }

        //		public static void ParseCraftFiles ()
        //		{
        //			// This run per save
        //			// Overwrite the existing data for this save
        //			listEditorVessel = new Dictionary<string, Dictionary<string, string>> ();
        //			Debug.Log ("[AH] Starting to parse .craft files for save : " + loadedGame);
        //
        //			string loadedGameStr = loadedGame;
        //			if (loadedGame.Contains (" (SANDBOX)")) {
        //				loadedGameStr.Remove (loadedGame.IndexOf (" (SANDBOX)"));
        //			} else if (loadedGame.Contains (" (CAREER)")) {
        //				loadedGameStr.Remove (loadedGame.IndexOf (" (CAREER)"));
        //			} else if (loadedGame.Contains (" (SCIENCE)")) {
        //				loadedGameStr.Remove (loadedGame.IndexOf (" (SCIENCE)"));
        //			} else {
        //				Debug.Log ("[AH] the name of the save can't be parsed");
        //			}
        //
        //			DirectoryInfo shipsDir = new DirectoryInfo (RegisterToolbar.ApplicationRootPath + "saves/" + loadedGameStr + "/Ships");
        //			FileInfo[] craftFiles = shipsDir.GetFiles ();
        //
        //			foreach (FileInfo craft in craftFiles) {
        //				if (craft.Extension != "craft") {
        //					continue;
        //				}
        //
        //				ConfigNode craftConf = ConfigNode.Load (craft.FullName);
        //				string craftName = craftConf.GetValue ("ship");
        //				listEditorVessel.Add (craftName, new Dictionary<string, string> ());
        //				listEditorVessel [craftName].Add ("type", craftConf.GetValue ("type"));
        //
        //
        //			}
        //		}

        public static void UpdateLoadedGame()
        {
            if (!loadedOnce)
            {
                DoStart();
                return;
            }

            if (HighLogic.CurrentGame.Title != loadedGame)
            {
                SaveToFile();
                loadedGame = HighLogic.CurrentGame.Title;
                LoadFromFile(loadedGame);
            }
        }

        public static void SaveShip(string shipName, string type, string totalPower, string relayPower)
        {
            string pid = "";
            while (pid == "" || listEditorVessel.ContainsKey(pid))
            {
                pid = UnityEngine.Random.Range(1, 1000000).ToString();
            }

            listEditorVessel.Add(pid, new Dictionary<string, string>());
            listEditorVessel[pid].Add("name", shipName);
            listEditorVessel[pid].Add("type", type);
            listEditorVessel[pid].Add("powerTotal", totalPower);
            listEditorVessel[pid].Add("powerRelay", relayPower);
            listEditorVessel[pid].Add("saveDate", System.DateTime.Now.ToString());
            listEditorVessel[pid].Add("connectedTo", "");
            listEditorVessel[pid].Add("realSignal", "0");

            SaveToFile();
        }

        public static void RemoveShip(string shipPid)
        {
            if (listEditorVessel.ContainsKey(shipPid))
            {
                listEditorVessel.Remove(shipPid);
            }

            SaveToFile();
        }

        public static Dictionary<string, Dictionary<string, string>> GetShipList(bool editorShip, bool flyingShip, bool applyMod = false)
        {
            Debug.Log("GetShipList, editorShip: " + editorShip + ", flyingShip: " + flyingShip + ", applyMod: " + applyMod);

            Dictionary<string, Dictionary<string, string>> returnList = new Dictionary<string, Dictionary<string, string>>();

            if (editorShip)
            {
                foreach (KeyValuePair<string, Dictionary<string, string>> kvp in listEditorVessel)
                {
                    returnList.Add(kvp.Key, kvp.Value);
                    if (applyMod)
                    {
                        returnList[kvp.Key]["powerTotal"] = AHUtil.TruePower(Double.Parse(returnList[kvp.Key]["powerTotal"])).ToString("N0");
                        returnList[kvp.Key]["powerRelay"] = AHUtil.TruePower(Double.Parse(returnList[kvp.Key]["powerRelay"])).ToString("N0");
                    }
                }
            }
            if (flyingShip)
            {
                foreach (KeyValuePair<string, Dictionary<string, string>> kvp in listFlyingVessel)
                {
                    returnList.Add(kvp.Key, kvp.Value);
                }
            }

            return returnList;
        }

        public static List<Dictionary<string, string>> GetShipListAsList(bool editorShip, bool relay = false, string type = "")
        {
            List<Dictionary<string, string>> newList;

            if (editorShip)
            {
                newList = ShipListAsList(listEditorVessel);
            }
            else
            {
                newList = ShipListAsList(listFlyingVessel);
            }

            if (type != "")
            {
                newList = newList.FindAll(ls => ls["type"] == type);
            }

            if (relay)
            {
                newList = newList.FindAll(ls => Double.Parse(ls["powerRelay"]) > 0);
            }

            newList.Sort(CompareShip);
            return newList;
        }

        private static List<Dictionary<string, string>> ShipListAsList(Dictionary<string, Dictionary<string, string>> dict)
        {
            List<Dictionary<string, string>> newList = new List<Dictionary<string, string>>();

            foreach (KeyValuePair<string, Dictionary<string, string>> kvp in dict)
            {

                Dictionary<string, string> newDict = new Dictionary<string, string>(kvp.Value);
                newDict.Add("pid", kvp.Key);

                newList.Add(newDict);
            }
            return newList;
        }

        private static int CompareShip(Dictionary<string, string> a, Dictionary<string, string> b)
        {
            if (a == null)
            {
                if (b == null)
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }

            if (b == null)
            {
                return -1;
            }

            // Move up flight relay
            if (a["type"] != b["type"])
            {
                if (a["type"] == "Relay")
                {
                    return -1;
                }
                else if (b["type"] == "Relay")
                {
                    return 1;
                }
            }

            // Move up editor relay
            double aPowerRelay = Double.Parse(a["powerRelay"]);
            double bPowerRelay = Double.Parse(b["powerRelay"]);

            if (aPowerRelay != 0)
            {
                if (bPowerRelay == 0)
                {
                    return -1;
                }
            }
            else if (bPowerRelay != 0)
            {
                return 1;
            }

            // Compare power
            if (aPowerRelay == bPowerRelay)
            {

                double aPowerTotal = Double.Parse(a["powerTotal"]);
                double bPowerTotal = Double.Parse(b["powerTotal"]);

                if (aPowerTotal == bPowerTotal)
                {
                    return 0;
                }
                else if (aPowerTotal > bPowerTotal)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
            else if (aPowerRelay > bPowerRelay)
            {
                return 1;
            }
            else
            {
                return -1;
            }
        }
    }

    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    public class AHShipListListener : MonoBehaviour
    {

        public void Start()
        {
            AHShipList.UpdateLoadedGame();

            GameEvents.CommNet.OnNetworkInitialized.Add(CommNetInit);
        }

        public void OnDestroy()
        {
            GameEvents.CommNet.OnNetworkInitialized.Remove(CommNetInit);
        }

        private void CommNetInit()
        {
            // create the vessel list now but wait for the real signal

            //			Debug.Log ("[AH][ShipListener] Commnet is initialized");
            AHShipList.ParseFlyingVessel();
        }
    }

    [KSPAddon(KSPAddon.Startup.TrackingStation, false)]
    public class AHShipListListenerTR : MonoBehaviour
    {
        private bool isReady = false;

        public void Start()
        {
            if (HighLogic.CurrentGame.Mode == Game.Modes.MISSION_BUILDER)
            {
                Destroy(this);
            }

            AHShipList.shipListReady = false;

            GameEvents.CommNet.OnCommStatusChange.Add(CommNetChange);

            StartCoroutine("ParseVesselsConnection");
        }

        public void OnDestroy()
        {
            GameEvents.CommNet.OnCommStatusChange.Remove(CommNetChange);
        }

        private void CommNetChange(Vessel v, bool b)
        {
            //			Debug.Log ("[AH] CommNet Update for vessel : " + v.GetName ());

            if (isReady)
            {
                StartCoroutine("ParseOneVesselConnection", v);
            }
        }

        private IEnumerator ParseOneVesselConnection(Vessel v)
        {
            isReady = false;
            AHShipList.shipListReady = false;

            yield return new WaitForSeconds(.2f);

            MapObject originalTarget = PlanetariumCamera.fetch.target;
            float originalDistance = PlanetariumCamera.fetch.Distance;

            PlanetariumCamera.fetch.SetTarget(v.mapObject);
            PlanetariumCamera.fetch.SetTarget(originalTarget);
            PlanetariumCamera.fetch.SetDistance(originalDistance);

            yield return new WaitForSeconds(.2f);

            AHShipList.ComputeRealSignal(v);

            AHShipList.shipListReady = true;
            isReady = true;
        }

        private IEnumerator ParseVesselsConnection()
        {
            Dictionary<string, Dictionary<string, string>> ahShipList = AHShipList.GetShipList(false, true);
            List<Vessel> vesselList = FlightGlobals.Vessels.FindAll(v => ahShipList.ContainsKey(v.id.ToString()));
            //			WaitForSeconds timer = new WaitForSeconds (.2f);

            yield return new WaitForSeconds(.6f);
            MapObject originalTarget = PlanetariumCamera.fetch.target;
            float originalDistance = PlanetariumCamera.fetch.Distance;

            foreach (Vessel v in vesselList)
            {
                PlanetariumCamera.fetch.SetTarget(v.mapObject);
            }
            PlanetariumCamera.fetch.SetTarget(originalTarget);
            PlanetariumCamera.fetch.SetDistance(originalDistance);

            yield return new WaitForSeconds(.2f);

            foreach (Vessel v in vesselList)
            {
                //				Debug.Log ("[AH] Computing real signal for : " + v.GetName ());
                AHShipList.ComputeRealSignal(v);
            }

            isReady = true;
            AHShipList.shipListReady = true;
            //			Debug.Log ("[AH] shipList is now ready");
            //			foreach (KeyValuePair<string, Dictionary<string, string>> kvp in AHShipList.GetShipList (false, true)) {
            //				Debug.Log ("[AH]");
            //				foreach (KeyValuePair<string, string> kvp2 in kvp.Value) {
            //					Debug.Log ("[AH] " + kvp2.Key + " : " + kvp2.Value);
            //				}
            //				Debug.Log ("[AH]");
            //			}
        }
    }
}

