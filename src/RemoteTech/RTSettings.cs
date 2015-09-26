﻿using System;
using System.Collections.Generic;
using UnityEngine;

namespace RemoteTech
{
    public class RTSettings
    {
        private static Settings mInstance;
        public static Settings Instance
        {
            get
            {
                if(mInstance is Settings && mInstance.settingsLoaded)
                {
                    return mInstance;
                }
                else
                {
                    return mInstance = Settings.Load();
                }
            }
        }
    }

    public class Settings
    {
        [Persistent] public bool RemoteTechEnabled = true;
        [Persistent] public float ConsumptionMultiplier = 1.0f;
        [Persistent] public float RangeMultiplier = 1.0f;
        [Persistent] public String ActiveVesselGuid = "35b89a0d664c43c6bec8d0840afc97b2";
        [Persistent] public float SpeedOfLight = 3e8f;
        [Persistent] public MapFilter MapFilter = MapFilter.Path | MapFilter.Omni | MapFilter.Dish;
        [Persistent] public bool EnableSignalDelay = true;
        [Persistent] public RangeModel.RangeModel RangeModelType = RangeModel.RangeModel.Standard;
        [Persistent] public double MultipleAntennaMultiplier = 0.0;
        [Persistent] public bool ThrottleTimeWarp = true;
        [Persistent] public bool ThrottleZeroOnNoConnection = true;
        [Persistent] public bool HideGroundStationsBehindBody = false;
        [Persistent] public Color DishConnectionColor = XKCDColors.Amber;
        [Persistent] public Color OmniConnectionColor = XKCDColors.BrownGrey;
        [Persistent] public Color ActiveConnectionColor = XKCDColors.ElectricLime;
        [Persistent] public Color RemoteStationColorDot = new Color(0.996078f, 0, 0, 1);
        [Persistent(collectionIndex="STATION")]
        public MissionControlSatellite[] GroundStations = new MissionControlSatellite[] { new MissionControlSatellite() };

        /// <summary>
        /// Trigger to force a reloading of the settings if a selected save is running.
        /// </summary>
        public bool settingsLoaded = false;
        public bool firstStart = false;

        /// <summary>
        /// Temp Variable for all the Window Positions for each instance.
        /// </summary>
        public Dictionary<String, Rect> savedWindowPositions = new Dictionary<String, Rect>();

        /// <summary>
        /// Backup config node
        /// </summary>
        private ConfigNode backupNode;

        /// <summary>
        /// Returns the current RemoteTech_Settings full path. The path will be empty
        /// if no save is loaded or the game is a training mission
        /// </summary>
        private static String File
        {
            get {

                if(HighLogic.CurrentGame == null || RTUtil.IsGameScenario)
                {
                    return "";
                }

                return KSPUtil.ApplicationRootPath + "/saves/" + HighLogic.SaveFolder + "/RemoteTech_Settings.cfg";
            }
        }

        /// <summary>
        /// Saves the current RTSettings object to the RemoteTech_Settings.cfg
        /// </summary>
        public void Save()
        {
            try
            {
                String settingsFile = Settings.File;

                // only save the settings if the file name is not empty (=not loading screen or training)
                if(!String.IsNullOrEmpty(settingsFile))
                {
                    ConfigNode details = new ConfigNode("RemoteTechSettings");
                    ConfigNode.CreateConfigFromObject(this, 0, details);
                    ConfigNode save = new ConfigNode();
                    save.AddNode(details);
                    save.Save(Settings.File);
                }
            }
            catch (Exception e) { RTLog.Notify("An error occurred while attempting to save: " + e.Message); }
        }

        /// <summary>
        /// Stores the MapFilter, ActiveVesselGuid and RemoteTechEnabled Value for overriding
        /// with third party settings
        /// </summary>
        public void backupFields()
        {
            backupNode = new ConfigNode();
            backupNode.AddValue("MapFilter", MapFilter);
            backupNode.AddValue("ActiveVesselGuid", ActiveVesselGuid);
            backupNode.AddValue("RemoteTechEnabled", RemoteTechEnabled);
        }

        /// <summary>
        /// Restores the backuped values from backupFields()
        /// </summary>
        public void restoreBackups()
        {
            if (backupNode != null)
            {
                // restore backups
                ConfigNode.LoadObjectFromConfig(this, backupNode);
            }
        }

        public static Settings Load()
        {
            // Create a new settings object
            Settings settings = new Settings();

            // Disable RemoteTech on Training missions
            if (RTUtil.IsGameScenario)
            {
                settings.RemoteTechEnabled = false;
            }

            // skip loading if we are on the loading screen
            // and return the default object and also for
            // scenario games.
            if (string.IsNullOrEmpty(Settings.File))
            {
                return settings;
            }

            settings.settingsLoaded = true;

            // try to load from the base settings.cfg
            ConfigNode load = ConfigNode.Load(Settings.File);

            if (load == null)
            {
                // write new base file to the rt folder
                settings.Save();
                settings.firstStart = true;
            }
            else
            {
                // old or new format?
                if (load.HasNode("RemoteTechSettings"))
                {
                    load = load.GetNode("RemoteTechSettings");
                }
                RTLog.Notify("Load base settings into object with {0}", load);
                // load basic file
                ConfigNode.LoadObjectFromConfig(settings, load);
            }

            // Prefer to load from GameDatabase, to allow easier user customization
            UrlDir.UrlConfig[] configList = GameDatabase.Instance.GetConfigs("RemoteTechSettings");
            foreach (UrlDir.UrlConfig curSet in configList)
            {
                // only third party files
                if (!curSet.url.Equals("RemoteTech/RemoteTech_Settings/RemoteTechSettings"))
                {
                    RTLog.Notify("Override RTSettings with configs from {0}", curSet.url);
                    settings.backupFields();
                    ConfigNode.LoadObjectFromConfig(settings, curSet.config);
                    settings.restoreBackups();
                }
            }

            return settings;
        }
    }
}