using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MahorobaDownloader.Entities
{
	[JsonObject("Stamp")]
	public class Stamp
	{
		public Stamp()
		{
		}

		public Stamp(string id)
		{
			Id = id;
		}

		[JsonProperty("id")]
		public string Id { get; set; }
	}
}
