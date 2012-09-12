using System;
using System.Runtime.InteropServices;

namespace CAresSharp
{
	unsafe internal struct ares_txt_reply {
		public ares_txt_reply *next;
		public IntPtr txt;
		public IntPtr txtlength;

		unsafe static int length(ares_txt_reply *reply)
		{
			int n = 0;
			for (ares_txt_reply *i = reply; i != null; i = i->next) {
				n++;
			}
			return n;
		}

		unsafe public static string[] convert(IntPtr ptr)
		{
			ares_txt_reply *reply = (ares_txt_reply *)ptr;
			int n = length(reply);
			int j = 0;
			string[] res = new string[n];
			for (ares_txt_reply *i = reply; i != null; i = i->next) {
				res[j] = Marshal.PtrToStringAnsi(i->txt, (int)i->txtlength);
				j++;
			}
			free(reply);
			return res;
		}

		unsafe static void free(ares_txt_reply *reply)
		{
			if (reply == null) {
				return;
			}
			free(reply->next);
			CAresChannel.ares_free_data((IntPtr)reply);
		}
	}
}

