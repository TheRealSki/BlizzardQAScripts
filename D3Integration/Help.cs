using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace integrate
{
    class Help
    {
        #region Properties
        public bool IsBetaAchieve { get; set; }
        public bool IsExit { get; set; }
        public bool IsLocal { get; set; }
        public bool IsLog { get; set; }
        public bool IsNoP4 { get; set; }
        public bool IsPullAll { get; set; }
        public bool IsNoSVN { get; set; }
        public bool IsRunVS { get; set; }
        public bool IsTime { get; set; }
        public bool IsUpdateOnly { get; set; }
        public string AuroraBuild { get; set; }
        public string LogLocation { get; set; }
        #endregion

        #region Declarations
        private static StringBuilder sb = new StringBuilder();
        private static Dictionary<Options, bool> startArgs = SetupArgs();

        private enum Options
        {
            HELP,                    //Displays a help file. (NYI)
            BETAACHIEVES,            //Uses beta achievements
            BUILDCODE,               //Builds using code to code directly (no install packages)
            INTEGRATIONTEST,         //Builds using Aurora_Integration_Test (predefined network address)
            INTEGRATION,             //Builds using Aurora_Installbuild (predefined network address)
            LOG,                     //Produce a log file
            NOP4,                    //If true, then p4 updating will be skipped (D3 specific)
            NOSVN,                   //If true, then SVN updating will be skipped (D3 specific)
            PULLALL,                 //If true, then all builds (not just successful builds) will be pulled from Jenkins
            RUNVS,                   //If true, will launch Visual Studio at end of integration
            TIME,                    //Times the entire integration process
            UPDATEONLY,              //Only updates the Perforce and SVN repositories (D3 specific)
            DEFAULT
        };
        #endregion

        #region Constructor
        public Help(string[] args)
        {
            //By default, we will integrate Aurora_Installbuild using normal achievement data (not Beta achievement data).

            //Setup the log
            GetDefaultLogLocation();

            AuroraBuild = "Aurora_Installbuild";
            if (args.Length == 0)
            {
                Log.Write("Integrating using " + AuroraBuild + ".");
                return;
            }

            ParseArguments(args);
            SetArgumentValues();

            if (IsLog)
            {
                var timeNow = System.DateTime.Now.TimeOfDay;
                string logName = AuroraBuild + "_integration_log_" +
                    timeNow.Hours.ToString() + "_" +
                    timeNow.Minutes.ToString() + "_" +
                    timeNow.Seconds.ToString() + ".txt";
                Log.swInitialize(System.IO.Path.Combine(LogLocation, logName));
            }
            Log.Write(sb.ToString());
        }
        #endregion

        #region Public Methods
        public void Finish()
        {
            if (IsRunVS)
            {
                //Since we're running VS after integration, Axe.exe and Axe.pdb should be made writable for building Axe.
                System.IO.FileInfo axeExe;
                System.IO.FileInfo axePdb;

                Log.Write("Removing Read Only attribute from Axe.exe and Axe.pdb.");

                try
                {
                    axeExe = new System.IO.FileInfo(System.IO.Directory.GetCurrentDirectory() + "\\main\\Debug\\Axe.exe");
                    axeExe.IsReadOnly = false;
                }
                catch (System.IO.FileNotFoundException fnfex)
                {
                    Log.Error(fnfex.Data + "\r\nThe file " + fnfex.FileName + " cannot be found." + "\r\nContinuing with integration.");
                }

                try
                {
                    axePdb = new System.IO.FileInfo(System.IO.Directory.GetCurrentDirectory() + "\\main\\Debug\\Axe.pdb");
                    axePdb.IsReadOnly = false;
                }
                catch (System.IO.FileNotFoundException fnfex)
                {
                    Log.Error(fnfex.Data + "\r\nThe file " + fnfex.FileName + " cannot be found." + "\r\nContinuing with integration.");
                }

                //Run VS
                ExternalProcess exProc = new ExternalProcess("C:\\Program Files (x86)\\Microsoft Visual Studio\\VERSION\\Common7\\IDE\\devenv.exe");
                exProc.WorkingDirectory += "\\main\\Code";
                exProc.Arguments = exProc.WorkingDirectory + "\\ProjectX_2008.sln";
                exProc.SetRedirect(false);
                exProc.Start();
            }
        }
        #endregion

        #region Private Methods
        private static Dictionary<Options, bool> SetupArgs()
        {
            Dictionary<Options, bool> newDict = new Dictionary<Options, bool>();
            newDict.Add(Options.HELP, false);
            newDict.Add(Options.BETAACHIEVES, false);
            newDict.Add(Options.BUILDCODE, false);
            newDict.Add(Options.INTEGRATIONTEST, false);
            newDict.Add(Options.INTEGRATION, false);
            newDict.Add(Options.LOG, false);
            newDict.Add(Options.NOP4, false);
            newDict.Add(Options.NOSVN, false);
            newDict.Add(Options.PULLALL, false);
            newDict.Add(Options.RUNVS, false);
            newDict.Add(Options.TIME, false);
            newDict.Add(Options.UPDATEONLY, false);
            newDict.Add(Options.DEFAULT, false);
            return newDict;
        }

        private static void SetArgumentValues()
        {
            foreach (KeyValuePair<Options, bool> kvp in startArgs)
            {
                switch (kvp.Key)
                {
                    case Options.HELP:
                    case Options.BETAACHIEVES:
                    case Options.BUILDCODE:
                    case Options.INTEGRATIONTEST:
                    case Options.INTEGRATION:
                    case Options.LOG:
                    case Options.NOP4:
                    case Options.NOSVN:
                    case Options.PULLALL:
                    case Options.RUNVS:
                    case Options.TIME:
                    case Options.UPDATEONLY:
                    default:
                        
                }
            }
        }
        #endregion
    }
}
