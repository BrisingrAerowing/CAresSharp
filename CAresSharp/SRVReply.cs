using System;

namespace CAresSharp
{
	unsafe internal struct ares_srv_reply
	{
		public ares_srv_reply *next;
		public sbyte *host;
		public ushort priority;
		public ushort weight;
		public ushort port;

		unsafe public static int length(ares_srv_reply *reply)
		{
			int n = 0;
			for (var i = reply; i != null; i = i->next) {
				n++;
			}
			return n;
		}

		unsafe public static SRVReply[] to_array(ares_srv_reply *reply)
		{
			int j = 0;
			var res = new SRVReply[ares_srv_reply.length(reply)];
			for (var i = reply; i != null; i = i->next) {
				res[j] = new SRVReply(i);
				j++;
			}
			free(reply);
			return res;
		}

		unsafe static void free(ares_srv_reply *reply)
		{
			if (reply == null) {
				return;
			}
			free(reply->next);
			CAresChannel.ares_free_data((IntPtr)reply);
		}
	};

	public class SRVReply
	{
		public SRVReply()
		{
		}

		unsafe internal SRVReply(ares_srv_reply *reply)
		{
			Weight = reply->weight;
			Priority = reply->priority;
			Port = reply->port;
			Host = new string(reply->host);
		}

		public int Weight { get; set; }
		public int Priority { get; set; }
		public int Port { get; set; }
		public string Host { get; set; }
	}
}

