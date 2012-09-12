using System;

namespace CAresSharp
{
	unsafe internal struct ares_soa_reply {
		public sbyte *nsname;
		public sbyte *hostmaster;
		public uint serial;
		public uint refresh;
		public uint retry;
		public uint expire;
		public uint minttl;
	}

	public class SOAReply
	{
		public SOAReply()
		{
		}

		unsafe internal SOAReply(ares_soa_reply *reply)
		{
			NSName = new string(reply->nsname);
			HostMaster = new string(reply->hostmaster);
			Serial = reply->serial;
			Refresh = reply->refresh;
			Retry = reply->retry;
			Expire = reply->expire;
			MinTTL = reply->minttl;
		}

		public string NSName { get; set; }
		public string HostMaster { get; set; }
		public uint Serial { get; set; }
		public uint Refresh { get; set; }
		public uint Retry { get; set; }
		public uint Expire { get; set; }
		public uint MinTTL { get; set; }
	}
}
