﻿using System;
using System.Threading;
using System.Globalization;
using CgfConverter;

namespace CgfConverterConsole
{
    public class Program
    {
        public static int Main(string[] args)
        {
            Utils.LogLevel = LogLevelEnum.Warning;
            Utils.DebugLevel = LogLevelEnum.Debug;

            string oldTitle = Console.Title;

#if DEV_DOLKENSP
            Utils.LogLevel = LogLevelEnum.None;      // Display NO error logs
            Utils.DebugLevel = LogLevelEnum.Debug;

            args = new String[] { @"O:\Mods\Models\*.cg?", @"O:\Mods\Models\*.skin", @"O:\Mods\Models\*.chr", "-objectdir", @"O:\Mods\SC\Latest", "-tif", "-merge", "-obj", "-outdir", @"O:\Mods\Models\Export" };
            args = new String[] { @"O:\Mods\SC\Latest\*.cg?", @"O:\Mods\SC\Latest\*.skin", @"O:\Mods\SC\Latest\*.chr", "-objectdir", @"O:\Mods\SC\Latest", "-tif", "-merge", "-obj", "-outdir", @"O:\Mods\Assets_Out" };
            args = new String[] { @"Objects\*.cg?", @"Objects\*.skin", @"Objects\*.chr", "-objectdir", @"O:\Mods\SC\Latest", "-tif", "-merge", "-obj", "-outdir", @"Export" };
            // args = new String[] { @"O:\Mods\Assets\*.cg?", "-objectdir", @"O:\Mods\SC\Latest", "-tif", "-merge", "-dae", "-outdir", @"O:\Mods\Assets_Out" };
            args = new String[] { @"Objects\*.cg?", @"Objects\*.skin", @"Objects\*.chr", "-objectdir", @"O:\Mods\SC\Latest", "-tif", "-merge", "-obj", "-cry", "-dae", "-outdir", @"Export", "-skipshield", "-skipproxy" };
            args = new String[] { @"Starfarer\*.cg?", "-objectdir", @"D:\Workspaces\github\Cryengine-Converter\cgf-converter\bin\dev_dolkensp", "-tif", "-merge", "-obj", "-cry", "-outdir", @"Export", "-skipshield", "-skipproxy" };
#endif

#if DEV_MARKEMP
            Utils.LogLevel = LogLevelEnum.Verbose; // Display ALL error logs in the console
            Utils.DebugLevel = LogLevelEnum.Debug;  // Send all to the IDE Output window

			//args = new String[] { @"D:\depot\mwo\Objects\mechs\catapult\body\catapult.chr", "-dds", "-dae", "-objectdir", @"d:\depot\mwo\" };
			//args = new String[] { @"D:\depot\mwo\Objects\environments\frontend\mechlab_a\mechbay_ceilings\mechbay_ceilinga.cgf", "-dds", "-dae", "-objectdir", @"d:\depot\mwo\" };
			//args = new String[] { @"D:\depot\mwo\Objects\environments\industrial\mf_maglev_loader_a.cgf", "-dds", "-dae", "-objectdir", @"d:\depot\mwo\" };
			//args = new String[] { @"D:\depot\MWO\Objects\characters\pilot\pilot_body.chr", "-dds", "-dae", "-objectdir", @"d:\blender projects\mechs\" };
			//args = new String[] { @"D:\depot\MWO\Objects\environments\city\im_roads_zone_05\im_zone05_block16.cgf", "-objectdir", @"d:\blender projects\mechs", "-dae" };
			//args = new String[] { @"D:\depot\SC\Data\Objects\animals\fish\Fish_JellyFish_prop_animal_01.cga", "-objectdir", @"d:\depot\sc\data", "-dae" };
			//args = new String[] { @"D:\depot\SC\Data\Objects\animals\fish\CleanerFish_clean_prop_animal_01.chr", "-objectdir", @"d:\depot\sc\data", "-dae", "-tif" };
			//args = new String[] { @"d:\depot\SC\Data\Objects\Spaceships\Ships\AEGS\Gladius\AEGS_Gladius.cga", "-objectdir", @"d:\depot\sc\data", "-dae", "-tif" };
			//args = new String[] { @"d:\depot\SC\Data\Objects\Spaceships\Ships\AEGS\Retaliator\AEGS_retaliator.cga", "-objectdir", @"d:\depot\sc\data", "-dae", "-tif" };
			//args = new String[] { @"D:\depot\SC\Data\Objects\Spaceships\Ships\AEGS\Redeemer\AEGS_Redeemer.cga", "-objectdir", @"d:\depot\sc\data", "-dae", "-tif" };
			//args = new String[] { @"D:\depot\SC\Data\Objects\Spaceships\Ships\DRAK\Cutlass\DRAK_Cutlass.cga", "-objectdir", @"d:\depot\sc\data", "-dae", "-tif" };
			//args = new String[] { @"d:\depot\SC\Data\Objects\buildingsets\human\lowtech\bravo\grimhex\anchor\anchor_gun_a.cgf", "-objectdir", @"d:\depot\sc\data", "-dds", "-dae", "-tif" };
			//args = new String[] { @"D:\depot\SC\Data\Objects\Characters\Human\heads\male\npc\male09\male09_t1_head.skin", "-objectdir", @"d:\depot\sc\data", "-dae", "-tif" };
			//args = new String[] { @"D:\depot\SC\Data\Objects\Characters\Human\male_v7\export\bhm_skeleton_v7.chr", "-objectdir", @"d:\depot\sc\data", "-dae", "-tif" };
			//args = new String[] { @"D:\depot\SC\Data\Objects\buildingsets\human\universal\org\trees\tree_ash_a.cgf", "-objectdir", @"d:\depot\sc\data", "-dae", "-tif" };
			//args = new String[] { @"D:\depot\SC\Data\Objects\Characters\Human\male_v7\armor\rsi\m_rsi_pilot_flightsuit_01.skin", "-objectdir", @"d:\depot\sc\data", "-dae", "-tif" };
			//args = new String[] { @"D:\depot\SC\Data\Objects\Characters\Human\male_v7\armor\slaver\m_slaver_medium_armor_01_core.skin", "-objectdir", @"d:\depot\sc\data", "-dae", "-tif" };
			//args = new String[] { @"d:\temp\prey\dahl_genmalebody01.skin", "-objectdir", @"d:\temp\prey", "-dae", "-dds" };
#endif

			ArgsHandler argsHandler = new ArgsHandler();
            int result = argsHandler.ProcessArgs(args);

#if !DEBUG
            try
            {
#endif
            CultureInfo customCulture = (CultureInfo)Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";

            Thread.CurrentThread.CurrentCulture = customCulture;

            if (result == 0)
            {
                foreach (String inputFile in argsHandler.InputFiles)
                {
                    try
                    {
                        // Read CryEngine Files
                        CryEngine cryData = new CryEngine(inputFile, argsHandler.DataDir.FullName);

                        #region Render Output Files

                        if (argsHandler.OutputBlender == true)
                        {
                            Blender blendFile = new Blender(argsHandler, cryData);

                            blendFile.Render(argsHandler.OutputDir, argsHandler.InputFiles.Count > 1);
                        }

                        if (argsHandler.OutputWavefront == true)
                        {
                            Wavefront objFile = new Wavefront(argsHandler, cryData);

                            objFile.Render(argsHandler.OutputDir, argsHandler.InputFiles.Count > 1);
                        }

                        if (argsHandler.OutputCryTek == true)
                        {
                            CryRender cryFile = new CryRender(argsHandler, cryData);

                            cryFile.Render(argsHandler.OutputDir, argsHandler.InputFiles.Count > 1);
                        }

                        if (argsHandler.OutputCollada == true)
                        {
                            COLLADA daeFile = new COLLADA(argsHandler, cryData);

                            daeFile.Render(argsHandler.OutputDir, argsHandler.InputFiles.Count > 1);
                        }

                        #endregion
                    }
                    catch (Exception ex)
                    {
                        Utils.Log(LogLevelEnum.Critical);
                        Utils.Log(LogLevelEnum.Critical, "********************************************************************************");
                        Utils.Log(LogLevelEnum.Critical, "There was an error rendering {0}", inputFile);
                        Utils.Log(LogLevelEnum.Critical);
                        Utils.Log(LogLevelEnum.Critical, ex.Message);
                        Utils.Log(LogLevelEnum.Critical);
                        Utils.Log(LogLevelEnum.Critical, ex.StackTrace);
                        Utils.Log(LogLevelEnum.Critical, "********************************************************************************");
                        Utils.Log(LogLevelEnum.Critical);
                    }
                }
            }

#if !DEBUG
            }
            catch (Exception)
            {
                if (argsHandler.Throw)
                    throw;
            }
#endif

            Console.Title = oldTitle;

#if (DEV_DOLKENSP || DEV_MARKEMP)
            Console.WriteLine("Done...");
            Console.ReadKey();
#endif

            return result;
        }
    }
}