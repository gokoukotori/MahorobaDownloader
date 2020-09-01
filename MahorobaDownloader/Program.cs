using log4net;
using MahorobaDownloader.Constants;
using MahorobaDownloader.Entities;
using MahorobaDownloader.Utils;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace MahorobaDownloader
{
	class Program
	{

		public static readonly string BASE_URL = @"https://cdn-static.prd.sakura.fusion-studio.co.jp/1.2.0/resources/";
		public static readonly string BASE_DIRECTORY = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName);
		public static readonly string BASE_JSON_DIRECTORY = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName, "Json");
		public static readonly string JSON_STAMP_PATH = Path.Combine(BASE_JSON_DIRECTORY, "StampList.json");
		public static readonly string LOG_CONFIG_FILE = Path.Combine(BASE_DIRECTORY, "log4net.config");
		private static ILog logger;

		static void Main(string[] args)
		{
			SetupLog4net();
			try
			{
				Init();

				var stamps = GetStamps(JSON_STAMP_PATH);

				logger.Info("スタンプ取得開始");
				foreach (var item in GetStampBinaries(stamps))
				{
					logger.Info("スタンプ取得中 ID: " + item.Key);
					FileUtils.WriteBinaryToFile(Path.Combine(BASE_DIRECTORY, "test", item.Key + ".png"), item.Value);
				}
				logger.Info("スタンプ取得終了");
			}catch(Exception ex)
			{
				logger.Fatal("致命的なエラーが発生");
				logger.Fatal(ex.Message);
				logger.Fatal(ex.StackTrace);
			}
			Console.ReadKey();
		}

		public static void SetupLog4net()
		{
			System.Xml.XmlDocument log4netConfig = new System.Xml.XmlDocument();
			log4netConfig.Load(File.OpenRead(LOG_CONFIG_FILE));
			var repo = LogManager.CreateRepository(
			 Assembly.GetEntryAssembly(),
			 typeof(log4net.Repository.Hierarchy.Hierarchy));
			log4net.Config.XmlConfigurator.Configure(repo, log4netConfig["log4net"]);
			logger = LogManager.GetLogger(typeof(Program));
			logger.Debug("Log4net利用可能");
		}
		public static void Init()
		{
			logger.Debug("初期化処理開始");
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			if (!File.Exists(JSON_STAMP_PATH))
			{
				logger.Debug("Json作成 Path:" + JSON_STAMP_PATH);
				string json = JsonConvert.SerializeObject(UseInitContent.SMTAMP_LIST, Formatting.Indented);
				FileUtils.WriteStringToFile(JSON_STAMP_PATH, json);
			}
			logger.Debug("初期化処理終了");
		}

		private static string[] GetStamps(string path)
		{
			logger.Debug("Json読込 Path:" + JSON_STAMP_PATH);
			var jsonstring = FileUtils.ReadStringFromFile(path);
			var stampList = JsonConvert.DeserializeObject<List<Stamp>>(jsonstring);
			return stampList.Select(x => x.Id).ToArray();
		}


		public static byte[] GetStampBinary(string stampId)
		{
			return GetStampBinaries(stampId).First().Value;
		}
		public static Dictionary<string, byte[]> GetStampBinaries(params string[] stampIds)
		{
			logger.Debug("スタンプバイナリ取得開始");
			var binaries = new Dictionary<string, byte[]>();
			using (var client = new WebClient())
			{
				foreach (var item in stampIds)
				{

					logger.Debug("スタンプバイナリ取得 Resources: " + "chat/" + item + ".png");
					if (binaries.ContainsKey(item)) continue;
					var hash = MD5ConvertTo("chat/" + item + ".png");
					binaries.Add(item, client.DownloadData(BASE_URL + hash + ".png"));
				}
			}

			logger.Debug("スタンプバイナリ取得終了");
			return binaries;
		}
		private static string MD5ConvertTo(string text)
		{
			logger.Debug("MD5化 text:" + text);
			//文字列をbyte型配列に変換する
			byte[] data = Encoding.UTF8.GetBytes(text);

			MD5 md5 = MD5.Create();

			//ハッシュ値を計算する
			byte[] bs = md5.ComputeHash(data);

			//リソースを解放する
			md5.Clear();

			//byte型配列を16進数の文字列に変換
			return BitConverter.ToString(bs).ToLower().Replace("-", "");
		}
	}
}
