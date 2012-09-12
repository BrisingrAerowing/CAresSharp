using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace CAresSharp
{
	unsafe struct hostent {
		public sbyte *name;
		public sbyte **aliases;
		public int addrtype;
		public int length;
		public sbyte **addrlist;

		public AddressFamily AddressFamily {
			get {
				return GetAddressFamily(addrtype);
			}
		}

		static AddressFamily GetAddressFamily(int addrtype)
		{
			switch (addrtype) {
			case 1:
				return AddressFamily.InterNetwork;
			case 28:
				return AddressFamily.InterNetworkV6;
			default:
				return AddressFamily.Unknown;
			}
		}

		static void Each(sbyte **iterator, Action<IntPtr> callback)
		{
			int i = 0;
			for (sbyte *j = iterator[0]; j != (sbyte *)0; j = iterator[++i]) {
				callback(new IntPtr(j));
			}
		}

		IPAddress[] ToAddress()
		{
			List<IPAddress> list = new List<IPAddress>();
			int size = 128;
			IntPtr dst = UV.Alloc(size);

			var that = this;

			hostent.Each(addrlist, (src) => {
				inet_ntop(that.addrtype, src, dst, new IntPtr(size));
				string ip = new string((sbyte *)dst.ToPointer());
				list.Add(IPAddress.Parse(ip));
			});

			UV.Free(dst);
			return list.ToArray();
		}

		string[] ToAliases()
		{
			List<string> l = new List<string>();
			hostent.Each(aliases, (src) => {
				l.Add(new string((sbyte *)src));
			});
			return l.ToArray();
		}

		public Hostent ToHostent()
		{
			return new Hostent(Hostname, ToAddress(), ToAliases());
		}

		[DllImport("cares")]
		public static extern void ares_free_hostent(IntPtr host);

		public static Hostent GetHostent(IntPtr ptr)
		{
			return ((hostent *)ptr)->ToHostent();
		}

		public static Hostent convert(IntPtr ptr)
		{
			var ret = GetHostent(ptr);
			ares_free_hostent(ptr);
			return ret;
		}

		public string Hostname {
			get {
				return new string(name);
			}
		}

		[DllImport("__Internal", CallingConvention = CallingConvention.Cdecl)]
		unsafe static extern sbyte *inet_ntop(int af, IntPtr src, IntPtr dst, IntPtr size);
	}
	
	public class Hostent
	{
		public Hostent(string hostname, IPAddress[] addresses, string[] aliases)
		{
			Hostname = hostname;
			IPAddresses = addresses;
			Aliases = aliases;
		}

		public string Hostname { get; protected set; }
		public IPAddress[] IPAddresses { get; protected set; }
		public string[] Aliases { get; protected set; }
	}
}
