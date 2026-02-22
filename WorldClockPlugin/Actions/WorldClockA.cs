namespace Loupedeck.WorldClockPlugin
{
    using System;
    using System.Globalization;
    using System.Collections.Generic;

    using Loupedeck.WorldClockPlugin.Helpers;
    using Loupedeck.WorldClockPlugin.l10n;

    using NodaTime;
    using NodaTime.Extensions;



    public class WorldClockA : PluginDynamicCommand
    {
        private WorldClockPlugin _plugin;
        private L10n _l10n;
        private Dictionary<String, String> l7dValues;
        private readonly System.Collections.Concurrent.ConcurrentDictionary<String, byte> _knownParams = new();
        public WorldClockA()
            : base() => this.MakeProfileAction("tree");
        protected override PluginProfileActionData GetProfileActionData()
        {
            var tree = new PluginProfileActionTree("Select location");
            tree.AddLevel(this.l7dValues["zone"]);
            tree.AddLevel(this.l7dValues["location"]);

            foreach (String zone in Globals.tzNames.AllKeys)
            {
                var node = tree.Root.AddNode(zone);
                foreach (String area in Globals.tzNames[zone].Split(","))
                {
                    node.AddItem($"{zone}/{area}", area.Replace("_", " "));
                }
            }
            return tree;
        }
        protected override Boolean OnLoad()
        {
            this._plugin = base.Plugin as WorldClockPlugin;
            if (this._plugin is null)
            {
                return false;
            }
            this._l10n = new L10n(this._plugin);
            this.l7dValues = this._l10n.GetL7dNames("timeA");
            if (this.l7dValues != null)
            {
                this.DisplayName = this.l7dValues["displayName"];
                this.Description = this.l7dValues["description"];
                this.GroupName = this.l7dValues["groupName"];
            }
            else
            {
                this.DisplayName = "Time Analog";
                this.GroupName = "Analog";
                this._plugin.Log.Info($"12S : l7dValues was empty or null: DisplayName: {this.l7dValues["displayName"]}, groupName: {this.l7dValues["groupName"]}.");
            }
            this._plugin.Tick += (sender, e) =>
            {
                foreach (var p in _knownParams.Keys)
                    this.ActionImageChanged(p);
            };
            return base.OnLoad();
        }

        protected override void RunCommand(String actionParameter) => this.ActionImageChanged(actionParameter);

        // update command image (nothing to update here per se, but that's called to draw whatever is shown on the Loupedeck)
        protected override BitmapImage GetCommandImage(String actionParameter, PluginImageSize imageSize)
        {
            using (var bitmapBuilder = new BitmapBuilder(imageSize))
            {
                bitmapBuilder.Clear(BitmapColor.Black);
                if (!String.IsNullOrEmpty(actionParameter))
                {
                    DateTimeZone zone = DateTimeZoneProviders.Tzdb[actionParameter];
                    ZonedClock clock = SystemClock.Instance.InZone(zone);
                    ZonedDateTime today = clock.GetCurrentZonedDateTime();
                    _knownParams.TryAdd(actionParameter, 0);
                    DateTime todayDt = today.LocalDateTime.ToDateTimeUnspecified();
                    Int32 idx = actionParameter.LastIndexOf("/");
                    var secHandLength = 35;
                    var minHandLength = 30;
                    var hrHandLength = 20;
                    Int32[] handCoord = new Int32[2];
                    var x1 = bitmapBuilder.Width * 0.5;
                    var y1 = bitmapBuilder.Width * 0.5;
                    bitmapBuilder.SetBackgroundImage(EmbeddedResources.ReadImage(EmbeddedResources.FindFile("watchface1.png")));
                    handCoord = HelperFunctions.MSCoord(Int32.Parse(todayDt.ToString("ss", CultureInfo.InvariantCulture)), secHandLength, bitmapBuilder.Width, bitmapBuilder.Height);
                    bitmapBuilder.DrawLine(handCoord[0], handCoord[1], (Int32)x1, (Int32)y1, new BitmapColor(255, 0, 0), 1);
                    handCoord = HelperFunctions.MSCoord(Int32.Parse(todayDt.ToString("mm", CultureInfo.InvariantCulture)), minHandLength, bitmapBuilder.Width, bitmapBuilder.Height);
                    bitmapBuilder.DrawLine(handCoord[0], handCoord[1], (Int32)x1, (Int32)y1, new BitmapColor(120, 120, 120), 2);
                    handCoord = HelperFunctions.HrCoord(Int32.Parse(todayDt.ToString("hh", CultureInfo.InvariantCulture)) % 12, Int32.Parse(todayDt.ToString("mm", CultureInfo.InvariantCulture)), hrHandLength, bitmapBuilder.Width, bitmapBuilder.Height);
                    bitmapBuilder.DrawLine(handCoord[0], handCoord[1], (Int32)x1, (Int32)y1, new BitmapColor(120, 120, 120), 3);
                }
                return bitmapBuilder.ToImage();
            }
        }
    }
}
