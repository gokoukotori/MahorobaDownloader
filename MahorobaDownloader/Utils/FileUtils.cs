using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MahorobaDownloader.Utils
{
	public static class FileUtils
	{

		private static ILog logger = LogManager.GetLogger(typeof(FileUtils));


		public static void WriteStringToFile(string path, string data)
		{

			WriteBinaryToFile(path, Encoding.UTF8.GetBytes(data));
		}

		public static void WriteBinaryToFile(string path, byte[] data)
		{
			logger.Debug("ファイル書込 Path; " + path);
			var dir = Path.GetDirectoryName(path);
			if (!Directory.Exists(dir))
			{
				Directory.CreateDirectory(dir);
			}

			using var fs = new FileStream(path, FileMode.Create);
			using var sw = new BinaryWriter(fs);
			sw.Write(data);
		}

		public static string ReadStringFromFile(string path)
		{
			var data = ReadBinaryFromFile(path);
			if (data == null)
			{
				return null;
			}
			return Encoding.UTF8.GetString(data);
		}

		public static byte[] ReadBinaryFromFile(string path)
		{
			logger.Debug("ファイル読込 Path; " + path);
			if (File.Exists(path))
			{
				using var fs = new FileStream(path, FileMode.Open);
				using var sr = new BinaryReader(fs);
				var len = (int)fs.Length;
				var data = new byte[len];
				sr.Read(data, 0, len);

				return data;
			}

			return null;
		}
	}
}
