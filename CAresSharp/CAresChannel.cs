using System;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Text;

namespace CAresSharp
{
/*%
 * Currently defined type values for resources and queries.
 */
	enum ns_type : int {
		ns_t_invalid = 0,	/*%< Cookie. */
		ns_t_a = 1,		/*%< Host address. */
		ns_t_ns = 2,		/*%< Authoritative server. */
		ns_t_md = 3,		/*%< Mail destination. */
		ns_t_mf = 4,		/*%< Mail forwarder. */
		ns_t_cname = 5,		/*%< Canonical name. */
		ns_t_soa = 6,		/*%< Start of authority zone. */
		ns_t_mb = 7,		/*%< Mailbox domain name. */
		ns_t_mg = 8,		/*%< Mail group member. */
		ns_t_mr = 9,		/*%< Mail rename name. */
		ns_t_null = 10,		/*%< Null resource record. */
		ns_t_wks = 11,		/*%< Well known service. */
		ns_t_ptr = 12,		/*%< Domain name pointer. */
		ns_t_hinfo = 13,	/*%< Host information. */
		ns_t_minfo = 14,	/*%< Mailbox information. */
		ns_t_mx = 15,		/*%< Mail routing information. */
		ns_t_txt = 16,		/*%< Text strings. */
		ns_t_rp = 17,		/*%< Responsible person. */
		ns_t_afsdb = 18,	/*%< AFS cell database. */
		ns_t_x25 = 19,		/*%< X_25 calling address. */
		ns_t_isdn = 20,		/*%< ISDN calling address. */
		ns_t_rt = 21,		/*%< Router. */
		ns_t_nsap = 22,		/*%< NSAP address. */
		ns_t_nsap_ptr = 23,	/*%< Reverse NSAP lookup (deprecated). */
		ns_t_sig = 24,		/*%< Security signature. */
		ns_t_key = 25,		/*%< Security key. */
		ns_t_px = 26,		/*%< X.400 mail mapping. */
		ns_t_gpos = 27,		/*%< Geographical position (withdrawn). */
		ns_t_aaaa = 28,		/*%< Ip6 Address. */
		ns_t_loc = 29,		/*%< Location Information. */
		ns_t_nxt = 30,		/*%< Next domain (security). */
		ns_t_eid = 31,		/*%< Endpoint identifier. */
		ns_t_nimloc = 32,	/*%< Nimrod Locator. */
		ns_t_srv = 33,		/*%< Server Selection. */
		ns_t_atma = 34,		/*%< ATM Address */
		ns_t_naptr = 35,	/*%< Naming Authority PoinTeR */
		ns_t_kx = 36,		/*%< Key Exchange */
		ns_t_cert = 37,		/*%< Certification record */
		ns_t_a6 = 38,		/*%< IPv6 address (deprecated, use ns_t_aaaa) */
		ns_t_dname = 39,	/*%< Non-terminal DNAME (for IPv6) */
		ns_t_sink = 40,		/*%< Kitchen sink (experimentatl) */
		ns_t_opt = 41,		/*%< EDNS0 option (meta-RR) */
		ns_t_apl = 42,		/*%< Address prefix list (RFC3123) */
		ns_t_tkey = 249,	/*%< Transaction key */
		ns_t_tsig = 250,	/*%< Transaction signature. */
		ns_t_ixfr = 251,	/*%< Incremental zone transfer. */
		ns_t_axfr = 252,	/*%< Transfer zone of authority. */
		ns_t_mailb = 253,	/*%< Transfer mailbox records. */
		ns_t_maila = 254,	/*%< Transfer mail agent records. */
		ns_t_any = 255,		/*%< Wildcard match. */
		ns_t_zxfr = 256,	/*%< BIND-specific, nonstandard. */
		ns_t_max = 65536
	};
	
	/// <summary>
	/// class holding alloc and free functions
	/// </summary>
	class UV
	{
		public static IntPtr Alloc(int size)
		{
			return Marshal.AllocHGlobal(size);
		}

		public static void Free(IntPtr ptr)
		{
			Marshal.FreeHGlobal(ptr);
		}
	}

	public class CAresChannel : IDisposable
	{
		IntPtr channel;
		public IntPtr Handle {
			get {
				return channel;
			}
		}
		
		[DllImport("cares")]
		static extern int ares_init(ref IntPtr channel);
		
		void Init()
		{
			int ret = ares_init(ref channel);
			Ensure.Success(ret);
		}
		
		[DllImport("cares")]
		static extern int ares_init_options(ref IntPtr channel, ref ares_options options, int optmask);
		
		void Init(CAresChannelOptions options)
		{
			var ops = options.ToStruct(this);
			int ret = ares_init_options(ref channel, ref ops, (int)ARES_OPT.SOCK_STATE_CB);
			Ensure.Success(ret);
		}
		
		public CAresChannel()
		{
			Init();
		}
		
		public CAresChannel(CAresChannelOptions options)
		{
			Init(options);
		}
		
		~CAresChannel()
		{
			Dispose(false);
		}
	
		public void Dispose()
		{
			Dispose(true);
		}
		
		[DllImport("cares")]
		static extern void ares_destroy(IntPtr channel);
		
		void Destroy()
		{
			if (channel != IntPtr.Zero) {
				ares_destroy(channel);	
				channel = IntPtr.Zero;
			}
		}
		
		protected virtual void Dispose(bool disposing)
		{
			if (disposing) {
				GC.SuppressFinalize(this);
			}
			
			Destroy();
		}

		delegate int AresParseDelegate(IntPtr buf, int alen, out IntPtr host);

		static void Parse<T>(IntPtr arg, IntPtr abuf, int alen, AresParseDelegate parse, Func<IntPtr, T> convert) where T : class
		{
			var cb = Callback.GetObject<AresCallback<T>>(arg);
			IntPtr ptr;
			var ret = parse(abuf, alen, out ptr);
			if (ret != 0) {
				cb.End(Ensure.Exception(ret), null);
			} else {
				cb.End(null, convert(ptr));
			}
		}

		static void Parse(IntPtr arg, IntPtr abuf, int alen, AresParseDelegate parse)
		{
			Parse<Hostent>(arg, abuf, alen, parse, hostent.convert);
		}

		[DllImport("cares")]
		static extern void ares_query(IntPtr channel, string name, int dnsclass, ns_type type, Action<IntPtr, int, int, IntPtr, int> callback, IntPtr arg);

		static void query<T>(IntPtr channel, string name, int dnsclass, ns_type type, Action<IntPtr, int, int, IntPtr, int> callback, Action<Exception, T> clrcb) where T : class
		{
			AresCallback<T> cb = new AresCallback<T>(clrcb);
			ares_query(channel, name, dnsclass, type, callback, cb.Handle);
		}

		public void Resolve(string host, AddressFamily addressFamily, Action<Exception, Hostent> callback)
		{
			switch (addressFamily) {
			case AddressFamily.InterNetwork:
			case AddressFamily.InterNetworkV6:
				var cb = new AresCallback<Hostent>(callback);
				if (addressFamily == AddressFamily.InterNetwork) {
					ares_query(channel, host, 1, ns_type.ns_t_a, Callback4, cb.Handle);
				} else {
					ares_query(channel, host, 1, ns_type.ns_t_aaaa, Callback6, cb.Handle);
				}
				break;
			default:
				callback(new ArgumentException("addressFamily, protocol not supported"), null);
				break;
			}
		}

		#region ipv4

		public void Resolve(string host, Action<Exception, Hostent> callback)
		{
			Resolve(host, AddressFamily.InterNetwork, callback);
		}

		[DllImport("cares")]
		static extern int ares_parse_a_reply(IntPtr buf, int alen, out IntPtr host, IntPtr addrttls, IntPtr naddrttls);

		static int ares_parse_a_reply2(IntPtr buf, int alen, out IntPtr host)
		{
			return ares_parse_a_reply(buf, alen, out host, IntPtr.Zero, IntPtr.Zero);
		}

		static void Callback4(IntPtr arg, int status, int timeouts, IntPtr buf, int alen)
		{
			Parse(arg, buf, alen, ares_parse_a_reply2);
		}

		#endregion

		#region ipv6

		public void Resolve6(string host, Action<Exception, Hostent> callback)
		{
			Resolve(host, AddressFamily.InterNetworkV6, callback);
		}

		[DllImport("cares")]
		static extern int ares_parse_aaaa_reply(IntPtr buf, int alen, out IntPtr host, IntPtr addrttls, IntPtr naddrttls);

		static int ares_parse_aaaa_reply2(IntPtr buf, int alen, out IntPtr host)
		{
			return ares_parse_aaaa_reply(buf, alen, out host, IntPtr.Zero, IntPtr.Zero);
		}

		static void Callback6(IntPtr arg, int status, int timeouts, IntPtr buf, int alen)
		{
			Parse(arg, buf, alen, ares_parse_aaaa_reply2);
		}

		#endregion

		[DllImport("cares")]
		internal static extern void ares_free_data(IntPtr ptr);

		#region MX

		public void ResolveMX(string host, Action<Exception, MailExchange[]> callback)
		{
			query(channel, host, 1, ns_type.ns_t_mx, CallbackMX, callback);
		}
		
		[DllImport("cares")]
		unsafe static extern int ares_parse_mx_reply(IntPtr abuf, int alen, out IntPtr mx_out);
		
		static void CallbackMX(IntPtr arg, int status, int timeouts, IntPtr abuf, int alen)
		{
			Parse<MailExchange[]>(arg, abuf, alen, ares_parse_mx_reply, ares_mx_reply.convert);
		}

		#endregion
		
		#region NS

		public void ResolveNS(string host, Action<Exception, Hostent> callback)
		{
			query(channel, host, 1, ns_type.ns_t_ns, CallbackNS, callback);
		}

		[DllImport("cares")]
		unsafe static extern int ares_parse_ns_reply(IntPtr abuf, int alen, out IntPtr host);

		static void CallbackNS(IntPtr arg, int status, int timeouts, IntPtr abuf, int alen)
		{
			Parse(arg, abuf, alen, ares_parse_ns_reply);
		}

		#endregion
/*
		#region PTR
		#endregion
*/

		#region SOA

		public void ResolveSOA(string host, Action<Exception, SOAReply> callback)
		{
			query(channel, host, 1, ns_type.ns_t_soa, CallbackSOA, callback);
		}

		[DllImport("cares")]
		unsafe static extern int ares_parse_soa_reply(IntPtr abuf, int alen, out IntPtr reply);

		unsafe static void CallbackSOA(IntPtr arg, int status, int timeouts, IntPtr abuf, int alen)
		{
			Parse<SOAReply>(arg, abuf, alen, ares_parse_soa_reply, ares_soa_reply.convert);
		}

		#endregion

		#region SRV

		public void ResolveSRV(string host, Action<Exception, SRVReply[]> callback)
		{
			query(channel, host, 1, ns_type.ns_t_srv, CallbackSRV, callback);
		}

		[DllImport("cares")]
		unsafe static extern int ares_parse_srv_reply(IntPtr abuf, int alen, out IntPtr reply);

		unsafe static void CallbackSRV(IntPtr arg, int status, int timeouts, IntPtr abuf, int alen)
		{
			Parse<SRVReply[]>(arg, abuf, alen, ares_parse_srv_reply, ares_srv_reply.convert);
		}
		#endregion

		#region TXT

		public void ResolveTXT(string host, Action<Exception, string[]> callback)
		{
			query(channel, host, 1, ns_type.ns_t_txt, CallbackTXT, callback);
		}

		[DllImport("cares")]
		unsafe static extern int ares_parse_txt_reply(IntPtr abuf, int alen, out IntPtr reply);

		unsafe static void CallbackTXT(IntPtr arg, int status, int timeouts, IntPtr abuf, int alen)
		{
			Parse<string[]>(arg, abuf, alen, ares_parse_txt_reply, ares_txt_reply.convert);
		}

		#endregion

		[DllImport("cares")]
		static extern void ares_cancel(IntPtr channel);
		
		public void Cancel()
		{
			ares_cancel(channel);
		}
		
		[DllImport("cares")]
		static extern void ares_process_fd(IntPtr channel, int read_fd, int write_fd);
		
		public void Process(int readSocket, int writeSocket)
		{
			ares_process_fd(channel, readSocket, writeSocket);
		}
	}
}
