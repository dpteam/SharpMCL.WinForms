﻿using Ionic.Zip;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace SharpMCL
{
    static class Program
    {
        private const string msg_AnyKey = "Press any key to close console...";
        private const string msg_Error = "SharpMCL: Error";
        static string assetIndex = "";
		static string clientjar;

        static public Form LauncherForm1;
        static public Form CreatePackForm1;
        static public Form KonamiForm1;

        [STAThread]
		static void Main(string[] args)
        {
            Marshal.PrelinkAll(typeof(Program));
            Trace.AutoFlush = true;
            Trace.Listeners.Clear();
            Trace.Listeners.Add(new ConsoleTraceListener());
            Trace.Listeners.Add(new TextWriterTraceListener(System.IO.Path.GetFileNameWithoutExtension(typeof(Program).Assembly.GetName().Name) + ".log"));
			Process currentProcess = Process.GetCurrentProcess();
			currentProcess.PriorityClass = ProcessPriorityClass.High;
			Console.BackgroundColor = ConsoleColor.Black;
			Console.ForegroundColor = ConsoleColor.White;
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			try
            {
                LauncherForm1 = new LauncherForm();
                CreatePackForm1 = new CreatePackForm();
                KonamiForm1 = new KonamiForm();
                Application.Run(new LauncherForm());
			}
			catch (Exception value)
			{
				try
                {
                    MessageBox.Show(value.ToString(), caption: msg_Error);
				}
				catch
				{
				}
			}
		}

		public static void start(string clientdir, string client, string user, string uuid, string session)
		{
			string nfolder = clientdir + @"versions\" + client + @"\natives";
			Directory.CreateDirectory(nfolder);
			clientjar = client;
			JObject versions = json(clientdir, client);
			assetInd(versions);
			JArray libraries = (JArray)versions["libraries"];
			string assetsdir = "assets";
			string gameassets = assetsdir + @"\virtual\legacy";
			string url = @"-Xmx3G -Xms3G -Xmn128m -XX:+DisableExplicitGC -XX:+UseConcMarkSweepGC -XX:+UseParNewGC -XX:+UseNUMA -XX:+CMSParallelRemarkEnabled -XX:MaxTenuringThreshold=15 -XX:MaxGCPauseMillis=30 -XX:GCPauseIntervalMillis=150 -XX:+UseAdaptiveGCBoundary -XX:-UseGCOverheadLimit -XX:+UseBiasedLocking -XX:SurvivorRatio=8 -XX:TargetSurvivorRatio=90 -XX:MaxTenuringThreshold=15 -Dfml.ignorePatchDiscrepancies=true -Dfml.ignoreInvalidMinecraftCertificates=true -XX:+UseFastAccessorMethods -XX:+UseCompressedOops -XX:+OptimizeStringConcat -XX:+AggressiveOpts -XX:ReservedCodeCacheSize=1024m -XX:+UseCodeCacheFlushing -XX:SoftRefLRUPolicyMSPerMB=2000 -XX:ParallelGCThreads=10 -Dfml.ignorePatchDiscrepancies=true -Dfml.ignoreInvalidMinecraftCertificates=true -Djava.library.path=versions\" + client + @"\natives -cp ";
			url = libs(libraries, url, clientdir, client, client);
			url += @"versions\" + clientjar + @"\" + clientjar + ".jar ";
			url += (string)versions["mainClass"] + " ";
			url += versions["minecraftArguments"].ToString().
			Replace("${auth_player_name}", user).
			Replace("${version_name}", client).
			Replace("${game_directory}", clientdir).
			Replace("${assets_root}", assetsdir).
			Replace("${game_assets}", gameassets).
			Replace("${assets_index_name}", assetIndex).
			Replace("${auth_uuid}", uuid).
			Replace("${auth_access_token}", session).
			Replace("${user_type}", "legacy").
			Replace("${version_type}", "release").
			Replace("${user_properties}", "{}");
			ProcessStartInfo Info = new ProcessStartInfo("java");
			Info.Arguments = url;
			Info.WorkingDirectory = clientdir;
			Info.UseShellExecute = false;
			Info.RedirectStandardOutput = true;
			Info.RedirectStandardError = true;
			Info.StandardOutputEncoding = Encoding.GetEncoding("CP866");
			Process process = new Process();
			StreamWriter streamWriter = File.CreateText(@"SharpMCL.log");
			process.StartInfo = Info;
			Action<object, DataReceivedEventArgs> actionWrite = (sender, e) =>
			{
				Console.WriteLine(e.Data);
				streamWriter.WriteLine(e.Data);
			};
			process.ErrorDataReceived += (sender, e) => actionWrite(sender, e);
			process.OutputDataReceived += (sender, e) => actionWrite(sender, e);
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			process.WaitForExit();
            Console.WriteLine(value: msg_AnyKey);
			Console.ReadKey();
		}

		static string libs(JArray libraries, string url, string clientdir, string client, string n)
		{
			foreach (var librari in libraries)
			{
				string[] libdir = librari["name"].ToString().Split(':');
				string libsdir = @"libraries\" + libdir[0].Replace(".", @"\") + @"\" + libdir[1] + @"\" + libdir[2] + @"\" + libdir[1] + "-" + libdir[2];
				if (librari["natives"] == null)
				{
					url += libsdir + ".jar;";
				}
				else
				{
					string natives = clientdir + libsdir + "-natives-windows.jar";
					string nfolder = clientdir + @"versions\" + n + @"\natives\";
					unzip(natives, nfolder);
				}
			}
			JObject versions = json(clientdir, client);
			string inheritsFrom = (string)versions["inheritsFrom"];
			if (versions["jar"] != null)
			{
				clientjar = (string)versions["jar"];
			}
			if (inheritsFrom != null)
			{
				versions = json(clientdir, inheritsFrom);
				libraries = (JArray)versions["libraries"];
				return url += libs(libraries, url, clientdir, inheritsFrom, n);
			}
			assetInd((JObject)versions["assetIndex"]);
			return url;
		}

		static JObject json(string clientdir, string client)
		{
			string json = clientdir + @"versions\" + client + @"\" + client + ".json";
			return JObject.Parse(File.ReadAllText(json));
		}

		static string assetInd(JObject versions)
		{
			if (versions["assetIndex"] != null)
			{
				assetIndex = (string)versions["assetIndex"]["id"];
			}
			else if (versions["assets"] != null)
			{
				assetIndex = (string)versions["assets"];
			}
			return assetIndex;
		}

		static void unzip(string zipdfile, string zipfolder)
		{
			try
			{
				using (ZipFile zip = ZipFile.Read(zipdfile))
				{
					foreach (ZipEntry ef in zip)
					{
						zip.ExtractAll(zipfolder, ExtractExistingFileAction.OverwriteSilently);
					}
				}
			}
			catch { }
		}
	}
}