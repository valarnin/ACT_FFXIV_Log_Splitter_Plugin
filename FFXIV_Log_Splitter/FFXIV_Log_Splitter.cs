using System;
using System.IO;
using System.Windows.Forms;
using Advanced_Combat_Tracker;

namespace FFXIV_Log_Splitter
{
    public class Log_Splitter_Plugin : IActPluginV1
    {
        private StreamWriter file;
        private LogLineEventDelegate del;
        private CombatToggleEventDelegate del2;

        public void DeInitPlugin()
        {
            ActGlobals.oFormActMain.BeforeLogLineRead -= del;
            ActGlobals.oFormActMain.OnCombatEnd -= del2;
            file.Close();
        }

        public void InitPlugin(TabPage pluginScreenSpace, Label pluginStatusText)
        {
            ((TabControl)pluginScreenSpace.Parent).TabPages.Remove(pluginScreenSpace);
            var zone = ActGlobals.oFormActMain.CurrentZone;
            var dateTime = DateTime.UtcNow;
            var fileName = getFilename(zone, dateTime);
            file = new StreamWriter(fileName, true);
            file.AutoFlush = true;

            int encCount = 0;

            del2 = (bool isImport, CombatToggleEventArgs encounterInfo) =>
            {
                if (!isImport && encounterInfo.encounter.GetEncounterSuccessLevel() > 0)
                    ++encCount;
            };

            ActGlobals.oFormActMain.OnCombatEnd += del2;

            del = (bool isImport, LogLineEventArgs logInfo) =>
            {
                try
                {
                    var line = logInfo.originalLogLine;
                    var parts = line.Split('|');
                    var et = uint.Parse(parts[0]);
                    if (et == 1)
                    {
                        file.Close();
                        if (encCount < 1)
                        {
                            File.Delete(fileName);
                        }
                        encCount = 0;
                        zone = parts[3];
                        dateTime = DateTime.Parse(parts[1]).ToUniversalTime();
                        fileName = getFilename(zone, dateTime);
                        file = new StreamWriter(fileName, true);
                        file.AutoFlush = true;
                    }
                    file.WriteLine(line);
                }
                catch(Exception ex)
                {
                    Console.WriteLine("Exception: " + ex.ToString());
                }
            };

            ActGlobals.oFormActMain.BeforeLogLineRead += del;

            pluginStatusText.Text = "Initialized";
        }

        private string getFilename(string zone, DateTime dateTime)
        {
            var logFolder = Path.GetDirectoryName(ActGlobals.oFormActMain.LogFilePath);
            var name = dateTime.ToString("yyyy-MM-ddTHH\\_mm\\_ss.fffffffzzz") + "_" + zone;
            name = string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
            return logFolder + Path.DirectorySeparatorChar + "Zone_" + name + ".log";
        }
    }
}
