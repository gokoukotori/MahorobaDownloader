using MahorobaDownloader.Constants;
using MahorobaDownloader.Entities;
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

		public static readonly string JSON_STAMP_PATH = Path.Combine(BASE_DIRECTORY, "StampList.json");

		static void Main(string[] args)
		{
			init();
			var stamps = GetStamps(JSON_STAMP_PATH);
			//var directory = Path.Combine(Directory.GetParent(Assembly.GetExecutingAssembly().Location).FullName, "OUTPUT");
			//if (!Directory.Exists(directory))
			//{
			//	Directory.CreateDirectory(directory);
			//}
			var stampBinaries = GetStampBinaries(stamps);
			foreach (var item in stampBinaries)
			{
				WriteBinaryToFile(Path.Combine(BASE_DIRECTORY, "test" , item.Key + ".png"), item.Value);
			}
		}

		public static void init()
		{
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
			if (!File.Exists(JSON_STAMP_PATH))
			{
				string json = JsonConvert.SerializeObject(UseInitContent.SMTAMP_LIST, Formatting.Indented);
				WriteStringToFile(JSON_STAMP_PATH, json);
			}
		}

		private static string[] GetStamps(string path)
		{
			var jsonstring = ReadStringFromFile(path);
			var stampList = JsonConvert.DeserializeObject<List<Stamp>>(jsonstring);
			return stampList.Select(x => x.Id).ToArray();
		}

		private static string MD5ConvertTo(string text)
		{
			//文字列をbyte型配列に変換する
			byte[] data = Encoding.UTF8.GetBytes(text);

			MD5 md5 = MD5.Create();

			//ハッシュ値を計算する
			byte[] bs = md5.ComputeHash(data);

			//リソースを解放する
			md5.Clear();

			//byte型配列を16進数の文字列に変換
			return BitConverter.ToString(bs).ToLower().Replace("-","");
		}

		public static void WriteStringToFile(string path, string data)
		{

			WriteBinaryToFile(path, Encoding.UTF8.GetBytes(data));
		}

		public static string ReadStringFromFile(string path)
		{
			var data = ReadBinaryFromFile(path);
			if(data == null)
			{
				return null;
			}
			return Encoding.UTF8.GetString(data);
		}

		public static void WriteBinaryToFile(string path, byte[] data)
		{
			var dir = Path.GetDirectoryName(path);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			using (var fs = new FileStream(path, FileMode.Create))
			using (var sw = new BinaryWriter(fs))
			{
				sw.Write(data);
			}
		}

		public static byte[] ReadBinaryFromFile(string path)
		{
			if (File.Exists(path))
			{
				using (var fs = new FileStream(path, FileMode.Open))
				using (var sr = new BinaryReader(fs))
				{
					var len = (int)fs.Length;
					var data = new byte[len];
					sr.Read(data, 0, len);

					return data;
				}
			}

			return null;
		}

		public static byte[] GetStampBinary(string stampId)
		{
			return GetStampBinaries(stampId).First().Value;
		}
		public static Dictionary<string, byte[]> GetStampBinaries(params string[] stampIds)
		{
			var binaries = new Dictionary<string, byte[]>();
			using (var client = new WebClient())
			{
				foreach (var item in stampIds)
				{
					if (binaries.ContainsKey(item)) continue;
					var hash = MD5ConvertTo("chat/" + item + ".png");
					binaries.Add(item, client.DownloadData(BASE_URL + hash + ".png"));
				}
			}

			return binaries;
		}
	}
}
