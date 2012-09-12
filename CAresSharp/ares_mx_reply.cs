using System;
using System.Collections.Generic;

namespace CAresSharp
{
	unsafe struct ares_mx_reply {
		public ares_mx_reply *next;
		public sbyte *host;
		public ushort priority;

		public static MailExchange[] ToMailExchange(IntPtr reply)
		{
			List<MailExchange> list = new List<MailExchange>();
			ares_mx_reply *mx_reply = (ares_mx_reply *)reply;
			while (mx_reply != (ares_mx_reply *)0) {
				list.Add(new MailExchange(new string(mx_reply->host), mx_reply->priority));

				mx_reply = mx_reply->next;
			}
			return list.ToArray();
		}
	}
}

