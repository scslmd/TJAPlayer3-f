﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Net.Http;
using FDK;

namespace TJAPlayer3
{
	internal class Program
	{
		internal static string SkinName = "Unknown";
		internal static string SkinVersion = "Unknown";
		internal static string SkinCreator = "Unknown";
		internal static string Renderer = "Unknown";
		private static Mutex mutex;

		[STAThread]
		private static void Main()
		{
			mutex = new Mutex(false, "TJAPlayer3-f-Ver." + Assembly.GetExecutingAssembly().GetName().Version.ToString());

			if (mutex.WaitOne(0, false))
			{

				Trace.WriteLine("Current Directory: " + Environment.CurrentDirectory);
				Trace.WriteLine("EXEのあるフォルダ: " + AppContext.BaseDirectory);

				Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

				// BEGIN #23670 2010.11.13 from: キャッチされない例外は放出せずに、ログに詳細を出力する。
				// BEGIM #24606 2011.03.08 from: DEBUG 時は例外発生箇所を直接デバッグできるようにするため、例外をキャッチしないようにする。
				//2020.04.15 Mr-Ojii DEBUG 時も例外をキャッチするようにした。
				try
				{
					using (var mania = new TJAPlayer3())
						mania.Run();

					Trace.WriteLine("");
					Trace.WriteLine("Thank You For Playing!!!");
				}
				catch (Exception e)
				{
					Trace.WriteLine("");
					Trace.Write(e.ToString());
					Trace.WriteLine("");
					Trace.WriteLine("An error has occurred. Sorry.");
					AssemblyName asmApp = Assembly.GetExecutingAssembly().GetName();

					//情報リスト
					Dictionary<string, string> errorjsonobject = new Dictionary<string, string>
					{
						{ "Name",asmApp.Name },
						{ "Version",asmApp.Version.ToString() },
						{ "Exception",e.ToString() },
						{ "DateTime",DateTime.UtcNow.ToString("yyyy/MM/dd HH:mm:ss.ff") },
						{ "SkinName",SkinName },
						{ "SkinVersion",SkinVersion },
						{ "SkinCreator",SkinCreator },
						{ "Renderer",Renderer },
						{ "OperatingSystem",Environment.OSVersion.ToString() },
						{ "OSDescription",RuntimeInformation.OSDescription },
						{ "OSArchitecture",RuntimeInformation.OSArchitecture.ToString() },
						{ "RuntimeIdentifier",RuntimeInformation.RuntimeIdentifier },
						{ "FrameworkDescription",RuntimeInformation.FrameworkDescription },
						{ "ProcessArchitecture",RuntimeInformation.ProcessArchitecture.ToString() }
					};
					
					//エラーが発生したことをユーザーに知らせるため、HTMLを作成する。
					using (StreamWriter writer = new StreamWriter(AppContext.BaseDirectory + "Error.html", false, Encoding.UTF8))
					{
						writer.WriteLine("<html>");
						writer.WriteLine("<head>");
						writer.WriteLine("<meta http-equiv=\"content-type\" content=\"text/html\" charset=\"utf-8\">");
						writer.WriteLine("<style>");
						writer.WriteLine("<!--");
						writer.WriteLine("table{ border-collapse: collapse; } td,th { border: 2px solid; }");
						writer.WriteLine("-->");
						writer.WriteLine("</style>");
						writer.WriteLine("</head>");
						writer.WriteLine("<body>");
						writer.WriteLine("<h1>An error has occurred.(エラーが発生しました。)</h1>");
#if PUBLISH
						writer.WriteLine("<p>Error information has been sent.(エラー情報を送信しました。)</p>");
#else
						writer.WriteLine("<p>It is a local build, so it did not send any error information.(ローカルビルドのため、エラー情報を送信しませんでした。)</p>");
#endif
						writer.WriteLine("<table>");
						writer.WriteLine("<tbody>");
						writer.Write("<tr>");
						foreach (KeyValuePair<string, string> keyValuePair in errorjsonobject) 
						{
							writer.Write($"<th>{keyValuePair.Key}</th>");
						}
						writer.WriteLine("</tr>");
						writer.Write("<tr>");
						foreach (KeyValuePair<string, string> keyValuePair in errorjsonobject)
						{
							writer.Write($"<td>{keyValuePair.Value}</td>");
						}
						writer.WriteLine("</tr>");
						writer.WriteLine("</tbody>");
						writer.WriteLine("</table>");
						writer.WriteLine("</body>");
						writer.WriteLine("</html>");
					}
					CWebOpen.Open(AppContext.BaseDirectory + "Error.html");

#if PUBLISH
					//エラーの送信
					using (var client = new HttpClient())
					{
						var content = new StringContent(JsonSerializer.Serialize(errorjsonobject, new JsonSerializerOptions() { DictionaryKeyPolicy = new LowerCaseJsonNamingPolicy() }), Encoding.UTF8, "application/json");

						var resString = client.PostAsync("https://script.google.com/macros/s/AKfycbzPWvX1cd5aDcDjs0ohgBveIxBh6wZPvGk0Xvg7xFsEsoXXUFCSUeziaVsn7uoMtm_3/exec", content).Result;
					}
#endif
				}

				if (Trace.Listeners.Count > 1)
					Trace.Listeners.RemoveAt(1);

				mutex.ReleaseMutex();
				mutex = null;
			}
			else 
			{
				Console.WriteLine($"TJAPlayer3-f(Ver.{Assembly.GetExecutingAssembly().GetName().Version}) is already running.");
				Thread.Sleep(2000);
			}
		}

		//渡されたのをLowerCaseにして返します
		private class LowerCaseJsonNamingPolicy : JsonNamingPolicy 
		{
			public override string ConvertName(string name) 
				=> name.ToLower();
		}
	}
}
