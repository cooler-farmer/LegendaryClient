using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using LegendaryClient;
using LegendaryClient.Logic.UpdateRegion;

namespace TestApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            RiotPatcher p = new RiotPatcher(BaseUpdateRegion.GetUpdateRegion("Live"), Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));

            Stopwatch sw = new Stopwatch();
            sw.Start();
            var report = p.CheckUpdateRequirement().Result;
            sw.Stop();

            Console.WriteLine(@"Finished in {0} ms

Report Type:             {1}
Update Required:         {2}

Air Update Size:         {3} KB
Data Dragon Update Size: {4} KB
Total Updat Size:        {5} KB

Air Files To Update:     {6}

Air Installed Version:   {7}
Air Newest Version:      {8}

DD Installed Version:    {9}
DD Newest Version:       {10}

Theme:                   {11}",
                                sw.ElapsedMilliseconds, report.ReportType, report.UpdateRequired,
                                report.AirReport.UpdateSize, report.DataDragonReport.UpdateSize, report.TotalUpdateSize,
                                report.AirReport.FilesToUpdate.Length, report.AirReport.InstalledVersion, report.AirReport.NewestVersion,
                                report.DataDragonReport.InstalledVersion, report.DataDragonReport.NewestVersion, report.AirReport.Theme);

            Console.ReadLine();
        }
    }
}
