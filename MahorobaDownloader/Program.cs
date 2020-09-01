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
		public static readonly string BASE_OUTPUT_PATH = Path.Combine(BASE_DIRECTORY, "Output");
		public static readonly string OUTPUT_STAMP_PATH = Path.Combine(BASE_OUTPUT_PATH, "Stamp");
		public static readonly string OUTPUT_CHARACTERS_PATH = Path.Combine(BASE_OUTPUT_PATH, "Characters");
		public static readonly string BASE_JSON_DIRECTORY = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName, "Json");
		public static readonly string JSON_STAMP_PATH = Path.Combine(BASE_JSON_DIRECTORY, "StampList.json");
		public static readonly string JSON_CHARACTER_PATH = Path.Combine(BASE_JSON_DIRECTORY, "CharacterList.json");
		public static readonly string LOG_CONFIG_FILE = Path.Combine(BASE_DIRECTORY, "log4net.config");
		private static ILog logger;
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

		static void Main(string[] args)
		{
			SetupLog4net();
			try
			{
				Init();

				logger.Info("スタンプ取得開始");
				var stamps = GetStamps(JSON_STAMP_PATH);
				foreach (var item in GetStampBinaries(stamps))
				{
					logger.Info("スタンプ取得中 ID: " + item.Key);
					FileUtils.WriteBinaryToFile(Path.Combine(OUTPUT_STAMP_PATH, item.Key + ".png"), item.Value);
				}
				logger.Info("スタンプ取得終了");

				logger.Debug("キャラクター関連下準備開始");
				var characters = GetCharacters(JSON_CHARACTER_PATH);
				logger.Debug("キャラクター関連下準備終了");

				logger.Info("立ち絵取得開始");
				foreach (var item in characters)
				{
					if(File.Exists(Path.Combine(OUTPUT_CHARACTERS_PATH, item.Name,"立ち絵_" + item.PictureId + ".png")))
					{
						logger.Info("立ち絵取得済のためスキップ キャラクター画像ID: " + item.PictureId);
						continue;
					}

					logger.Info("立ち絵取得中 キャラクター画像ID: " + item.PictureId);
					byte[] binary;
					try
					{
						binary = GetStandingPictureBinary(item.PictureId);
					}
					catch (WebException ex)
					{
						HttpWebResponse response = (HttpWebResponse)ex.Response;
						if (response.StatusCode == HttpStatusCode.NotFound)
						{
							logger.Error("エラーが発生。立ち絵画像が存在しない、もしくはキャラクター画像IDは割り振られているが実装されていない可能性があります。");
						}
						else
						{
							logger.Error("エラーが発生。原因は不明です。");
						}
						logger.Error(ex.Message);
						logger.Error(ex.StackTrace);
						continue;
					}
					catch (Exception ex)
					{
						logger.Error("エラーが発生。原因は不明です。");
						logger.Error(ex.Message);
						logger.Error(ex.StackTrace);
						continue;
					}
					FileUtils.WriteBinaryToFile(Path.Combine(OUTPUT_CHARACTERS_PATH, item.Name,"立ち絵_" + item.PictureId + ".png"), binary);
				}
				logger.Info("立ち絵取得終了");


				logger.Info("エッチな絵取得開始");
				foreach (var item in characters)
				{
					if (File.Exists(Path.Combine(OUTPUT_CHARACTERS_PATH, item.Name, "エッチな絵_" + item.PictureId + "_1" + ".png")))
					{
						logger.Info("エッチな絵取得済のためスキップ キャラクター画像ID: " + item.PictureId);
						continue;
					}

					logger.Info("エッチな絵取得中 キャラクター画像ID: " + item.PictureId);
					Dictionary<string,byte[]> binary;
					try
					{
						binary = GetCharacterEroPictureBinary(item.PictureId);
					}
					catch (WebException ex)
					{
						HttpWebResponse response = (HttpWebResponse)ex.Response;
						if (response.StatusCode == HttpStatusCode.NotFound)
						{
							logger.Error("エラーが発生。エッチ過ぎな絵が存在しない、もしくはキャラクター画像IDは割り振られているが実装されていない可能性があります。");
						}
						else
						{
							logger.Error("エラーが発生。原因は不明です。");
						}
						logger.Error(ex.Message);
						logger.Error(ex.StackTrace);
						continue;
					}
					catch (Exception ex)
					{
						logger.Error("エラーが発生。原因は不明です。");
						logger.Error(ex.Message);
						logger.Error(ex.StackTrace);
						continue;
					}
					foreach (var eroImg in binary)
					{
						FileUtils.WriteBinaryToFile(Path.Combine(OUTPUT_CHARACTERS_PATH, item.Name, "エッチな絵_" + item.PictureId + "_" + eroImg.Key + ".png"), eroImg.Value);
					}
				}
				logger.Info("エッチな絵取得終了");

			}
			catch(Exception ex)
			{
				logger.Fatal("致命的なエラーが発生");
				logger.Fatal(ex.Message);
				logger.Fatal(ex.StackTrace);
			}

			logger.Info("処理が完了しました。キーを押して終了してください。");
			Console.ReadKey();
		}
		public static void Init()
		{
			logger.Debug("初期化処理開始");
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			string json = "";
			if (!File.Exists(JSON_STAMP_PATH))
			{
				logger.Debug("Json作成 Path:" + JSON_STAMP_PATH);
				json = JsonConvert.SerializeObject(UseInitContent.SMTAMP_LIST, Formatting.Indented);
				FileUtils.WriteStringToFile(JSON_STAMP_PATH, json);
			}
			if (!File.Exists(JSON_CHARACTER_PATH))
			{
				logger.Debug("Json作成 Path:" + JSON_CHARACTER_PATH);
				json = JsonConvert.SerializeObject(UseInitContent.CHARACTERS, Formatting.Indented);
				FileUtils.WriteStringToFile(JSON_CHARACTER_PATH, json);
			}
			logger.Debug("初期化処理終了");
		}

		#region キャラクター処理関連
		private static List<Character> GetCharacters(string path)
		{
			logger.Debug("Json読込 Path:" + path);
			var jsonstring = FileUtils.ReadStringFromFile(path);
			return JsonConvert.DeserializeObject<List<Character>>(jsonstring);
		}
		#endregion


		#region 立ち絵処理関連

		public static byte[] GetStandingPictureBinary(string characterPictureId)
		{
			return GetStandingPictureBinaries(characterPictureId).First().Value;
		}
		public static Dictionary<string, byte[]> GetStandingPictureBinaries(params string[] characterPictureIds)
		{
			logger.Debug("立ち絵バイナリ取得開始");
			var binaries = new Dictionary<string, byte[]>();
			using (var client = new WebClient())
			{
				foreach (var item in characterPictureIds)
				{

					logger.Debug("立ち絵バイナリ取得 Resources: " + "hero/big/" + item + ".png");
					if (binaries.ContainsKey(item)) continue;
					var hash = MD5ConvertTo("hero/big/" + item + ".png");
					binaries.Add(item, client.DownloadData(BASE_URL + hash + ".png"));
				}
			}

			logger.Debug("立ち絵バイナリ取得終了");
			return binaries;
		}


		#endregion


		#region エチエチエッチ絵処理関連
		public static Dictionary<string,byte[]> GetCharacterEroPictureBinary(string characterPictureId)
		{
			logger.Debug("エッチな絵のバイナリ取得開始");
			var binaries = new Dictionary<string, byte[]>();
			using (var client = new WebClient())
			{
				for (var index = 1; ; index++)
				{
					logger.Debug("エッチ過ぎな絵のバイナリ取得　 Resources: " + "hero/cg/" + characterPictureId + "_" + index + ".png");
					var hash = MD5ConvertTo("hero/cg/" + characterPictureId + "_" + index + ".png");
					try
					{
						var binary = client.DownloadData(BASE_URL + hash + ".png");
						binaries.Add(index.ToString(), binary);

					}
					catch (WebException ex)
					{
						HttpWebResponse response = (HttpWebResponse)ex.Response;
						if (index == 1 || response.StatusCode != HttpStatusCode.NotFound) throw;
						break;
					}
					catch (Exception ex)
					{
						throw;
					}
					logger.Debug("エッチ過ぎな絵のバイナリ取得② Resources: " + "hero/cg/" + characterPictureId + "_" + index + "_c.png");
					hash = MD5ConvertTo("hero/cg/" + characterPictureId + "_" + index + "_c.png");
					try
					{
						var binary = client.DownloadData(BASE_URL + hash + ".png");
						binaries.Add(index + "_c", binary);

					}
					catch (WebException ex)
					{
						HttpWebResponse response = (HttpWebResponse)ex.Response;
						if (response.StatusCode != HttpStatusCode.NotFound) throw;
					}
					catch (Exception ex)
					{
						throw;
					}
				}

				logger.Debug("エッチな絵のバイナリ取得終了");
				return binaries;
			}
		}

		#endregion


		#region スタンプ処理関連

		private static string[] GetStamps(string path)
		{
			logger.Debug("Json読込 Path:" + path);
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
		#endregion
	}
}
