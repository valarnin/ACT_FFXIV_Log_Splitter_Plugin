using System;
using System.IO;
using System.Windows.Forms;
using Advanced_Combat_Tracker;

namespace FFXIV_Log_Splitter
{
    public class Log_Splitter_Plugin : IActPluginV1
    {
        private StreamWriter file;
        private LogLineEventDelegate LogLineDel;
        private CombatToggleEventDelegate CombatDel;

        public void DeInitPlugin()
        {
            ActGlobals.oFormActMain.BeforeLogLineRead -= LogLineDel;
            ActGlobals.oFormActMain.OnCombatEnd -= CombatDel;
            file.Close();
            pluginStatusText.Text = "Powered Off";
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

            CombatDel = (bool isImport, CombatToggleEventArgs encounterInfo) =>
            {
                // Keep track of the number of encounter/combat
                if (!isImport && encounterInfo.encounter.GetEncounterSuccessLevel() > 0)
                    ++encCount;
            };

            ActGlobals.oFormActMain.OnCombatEnd += CombatDel;

            LogLineDel = (bool isImport, LogLineEventArgs logInfo) =>
            {
                try
                {
                    var line = logInfo.originalLogLine;
                    var parts = line.Split('|');
                    var lineID = uint.Parse(parts[0]);
                    
                    if (lineID == 1 && !parts[3].Equals(zone))
                    {
                        file.Close();
                        if (encCount < 1)
                        {
                            // If no combat in zone, delete log file
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

            ActGlobals.oFormActMain.BeforeLogLineRead += LogLineDel;

            pluginStatusText.Text = "Initialized";
        }

        private string getFilename(string zone, DateTime dateTime)
        {
            var logFolder = Path.GetDirectoryName(ActGlobals.oFormActMain.LogFilePath);
            var name = dateTime.ToString("yyyy-MM-ddTHH\\_mm\\_ss.fffffffzzz") + "_" + zone;
            name = string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
            return logFolder + Path.DirectorySeparatorChar + "Zone_" + name + ".zonelog";
        }
    }
}
