﻿using RomManagerShared.Base;
namespace RomManagerShared
{
    public class RomMissingUpdates
    {
        public RomMissingUpdates()
        {
        }
        {
            Rom = gameRom;
            LocalUpdates = localUpdates;
            LatestUpdate = latestUpdate;
        }
        public HashSet<Update>? LocalUpdates { get; set; }
        public Update? LatestUpdate { get; set; }//i thought of making it a list of updates with higher version but i think that's not important
        public override string ToString()
        {
            var report = $"Rom: {Rom} {Environment.NewLine}";
            if (LocalUpdates is null)
            {
                report += " No local updates" + Environment.NewLine;
            }
            else
            {
                foreach (var update in LocalUpdates)
                {
                    report += update.ToString() + Environment.NewLine;
                }
            }
            if (LatestUpdate is not null)
            {
                report += LatestUpdate.ToString() + Environment.NewLine;
            }
            return report;
        }
    }
    public class RomMissingDLCs
    {
        public RomMissingDLCs()
        {
        }
        {
            Rom = gameRom;
            LocalDLCs = [];
            MissingDLCs = [];
        }
        public HashSet<DLC>? LocalDLCs { get; set; }
        public HashSet<DLC>? MissingDLCs { get; set; }
        public override string ToString()
        {
            var report = $"Rom: {Rom} {Environment.NewLine}";
            if (LocalDLCs is null)
            {
                report += " No local DLCs" + Environment.NewLine;
            }
            else
            {
                foreach (var update in LocalDLCs)
                {
                    report += update.ToString() + Environment.NewLine;
                }
            }
            if (MissingDLCs is null)
            {
                report += " No missing DLCs" + Environment.NewLine;
            }
            else
            {
                foreach (var update in MissingDLCs)
                {
                    report += update.ToString() + Environment.NewLine;
                }
            }
            return report;
        }
    }
}