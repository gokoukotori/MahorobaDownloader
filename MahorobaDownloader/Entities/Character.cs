using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MahorobaDownloader.Entities
{
	[JsonObject("Character")]
	public class Character
	{
		public Character()
		{
		}

		public Character(string name, string pictureId)
		{
			Name = name;
			PictureId = pictureId;
		}

		[JsonProperty("name")]
		public string Name { get; set; }
		[JsonProperty("pictureId")]
		public string PictureId { get; set; }
	}
}
