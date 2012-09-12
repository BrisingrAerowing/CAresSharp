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
	
	
	class UV
	{
		public static IntPtr Alloc(int size)
		{
			return CAresChannel.DefaultMemoryFunctions.Alloc(size);
		}
		
		public static void Free(IntPtr ptr)
		{
			CAresChannel.DefaultMemoryFunctions.Free(ptr);
		}
	}
	
	public class MemoryFunctions
	{
		public delegate IntPtr AllocDelegate(int size);
		public delegate void FreeDelegate(IntPtr ptr);
		
		public AllocDelegate Alloc;
		public FreeDelegate Free;
		
		public MemoryFunctions(AllocDelegate alloc, FreeDelegate free)
		{
			Alloc = alloc;
			Free = free;
		}
	}
	
	public class CAresChannel : IDisposable
	{
		public static MemoryFunctions DefaultMemoryFunctions = new MemoryFunctions((size) => {
			return Marshal.AllocHGlobal(size);
		}, (ptr) => {
			Marshal.FreeHGlobal(ptr);
		});
		
		IntPtr channel;
		public IntPtr Handle {
			get {
				return channel;
			}
		}
		
		public MemoryFunctions MemoryFunctions { protected set; get; }
		
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
		
		
		/*
		public CAresChannel(MemoryFunctions memoryFunctions)
		{
			MemoryFunctions = memoryFunctions;
			
			Init();
		}
		
		public CAresChannel()
			: this(DefaultMemoryFunctions)
		{
		}
		*/
		
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
		
		
		delegate int AresLongParseDelegate(IntPtr buf, int alen, out IntPtr host, IntPtr addrttls, IntPtr naddrttls);

		static void LongParse(AresCallback<Hostent> cb, AresLongParseDelegate ares_parse, IntPtr buf, int alen)
		{
			IntPtr host;
			int r = ares_parse(buf, alen, out host, IntPtr.Zero, IntPtr.Zero);
			if (r != 0) {
				cb.End(Ensure.Exception(r), null);
			} else {
				var he = hostent.GetHostent(host);
				ares_free_hostent(host);
				cb.End(null, he);
			}
		}
		
		delegate int AresParseDelegate(IntPtr buf, int alen, out IntPtr host);
		
		static void Parse(AresCallback<Hostent> cb, AresParseDelegate ares_parse, IntPtr buf, int alen)
		{
			IntPtr host;
			int r = ares_parse(buf, alen, out host);
			if (r != 0) {
				cb.End(Ensure.Exception(r), null);
			} else {
				var he = hostent.GetHostent(host);
				ares_free_hostent(host);
				cb.End(null, he);
			}
		}

		[DllImport("cares")]
		static extern void ares_free_hostent(IntPtr host);

		[DllImport("cares")]
		static extern int ares_parse_a_reply(IntPtr buf, int alen, out IntPtr host, IntPtr addrttls, IntPtr naddrttls);

		static void Callback4(IntPtr arg, int status, int timeouts, IntPtr buf, int alen)
		{
			var cb = Callback.GetObject<AresCallback<Hostent>>(arg);
			LongParse(cb, ares_parse_a_reply, buf, alen);
		}

		[DllImport("cares")]
		static extern int ares_parse_aaaa_reply(IntPtr buf, int alen, out IntPtr host, IntPtr addrttls, IntPtr naddrttls);

		static void Callback6(IntPtr arg, int status, int timeouts, IntPtr buf, int alen)
		{
			var cb = Callback.GetObject<AresCallback<Hostent>>(arg);
			LongParse(cb, ares_parse_aaaa_reply, buf, alen);
		}

		[DllImport("cares")]
		static extern int ares_query(IntPtr channel, string name, int dnsclass, ns_type type, Action<IntPtr, int, int, IntPtr, int> callback, IntPtr arg);

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

		public void Resolve(string host, Action<Exception, Hostent> callback)
		{
			Resolve(host, AddressFamily.InterNetwork, callback);
		}

		public void Resolve6(string host, Action<Exception, Hostent> callback)
		{
			Resolve(host, AddressFamily.InterNetworkV6, callback);
		}

		public void ResolveMx(string host, Action<Exception, MailExchange[]> callback)
		{
			AresCallback<MailExchange[]> cb = new AresCallback<MailExchange[]>(callback);
			ares_query(channel, host, 1, ns_type.ns_t_mx, CallbackMx, cb.Handle);
		}
		
		[DllImport("cares")]
		unsafe static extern int ares_parse_mx_reply(IntPtr abuf, int alen, out IntPtr mx_out);
		
		[DllImport("cares")]
		static extern void ares_free_data(IntPtr ptr);

		static void CallbackMx(IntPtr arg, int status, int timeouts, IntPtr buf, int alen)
		{
			var cb = Callback.GetObject<AresCallback<MailExchange[]>>(arg);
			IntPtr reply;
			int r = ares_parse_mx_reply(buf, alen, out reply);
			if (r != 0) {
				cb.End(Ensure.Exception(r), null);
			} else {
				var me = ares_mx_reply.ToMailExchange(reply);
				ares_free_data(reply);
				cb.End(null, me);
			}
		}
		
/*
		#region NS
		
		public void ResolveNS(string host, Action<Exception, Hostent> callback)
		{
			AresCallback<Hostent> cb = new AresCallback<Hostent>(callback);
			ares_query(channel, host, 1, ns_type.ns_t_ns, CallbackNS, cb.Handle);
		}
		
		[DllImport("cares")]
		unsafe static extern int ares_parse_ns_reply(IntPtr abuf, int alen, out IntPtr host);

		static void CallbackNS(IntPtr arg, int status, int timeouts, IntPtr buf, int alen)
		{
			var cb = Callback.GetObject<AresCallback<Hostent>>(arg);
			Parse(cb, ares_parse_ns_reply, buf, alen);
		}
		
		#endregion
		
		#region PTR
		#endregion
		
		#region SOA
		#endregion

*/

		#region SRV

		unsafe struct ares_srv_reply {
		     public ares_srv_reply *next;
		     public sbyte *host;
		     public ushort priority;
		     public ushort weight;
		     public ushort port;
		};

		unsafe static int length(ares_srv_reply *reply) {
			int n = 0;
			for (var i = reply; i != null; i = i->next) {
				n++;
			}
			return n;
		}

		public void ResolveSRV(string host, Action<Exception, SRVReply[]> callback)
		{
			AresCallback<SRVReply[]> cb = new AresCallback<SRVReply[]>(callback);
			ares_query(channel, host, 1, ns_type.ns_t_srv, CallbackSRV, cb.Handle);
		}

		[DllImport("cares")]
		unsafe static extern int ares_parse_srv_reply(IntPtr abuf, int alen, out ares_srv_reply* reply);

		unsafe static void CallbackSRV(IntPtr arg, int status, int timeouts, IntPtr abuf, int alen)
		{
			var cb = Callback.GetObject<AresCallback<SRVReply[]>>(arg);
			ares_srv_reply *reply;
			int r = ares_parse_srv_reply(abuf, alen, out reply);
			if (r != 0) {
				cb.End(Ensure.Exception(r), null);
			} else {
				int j = 0;
				int n = length(reply);
				var res = new SRVReply[n];
				for (var i = reply; i != null; i = i->next) {
					res[j] = new SRVReply() {
						Weight = (int)(i->weight),
						Priority = (int)(i->priority),
						Port = (int)(i->port),
						Host = new string(i->host)
					};
					j++;
				}

				cb.End(null, res);
			}
		}
		#endregion

		#region TXT

		unsafe struct ares_txt_reply {
			public ares_txt_reply *next;
			public IntPtr txt;
			public IntPtr length;
		}

		public void ResolveTXT(string host, Action<Exception, string[]> callback)
		{
			AresCallback<string[]> cb = new AresCallback<string[]>(callback);
			ares_query(channel, host, 1, ns_type.ns_t_txt, CallbackTXT, cb.Handle);
		}

		[DllImport("cares")]
		unsafe static extern int ares_parse_txt_reply(IntPtr abuf, int alen, out ares_txt_reply* reply);

		unsafe static int length(ares_txt_reply *reply)
		{
			int n = 0;
			for (ares_txt_reply *i = reply; i != null; i = i->next) {
				n++;
			}
			return n;
		}

		unsafe static void CallbackTXT(IntPtr arg, int status, int timeouts, IntPtr abuf, int alen)
		{
			var cb = Callback.GetObject<AresCallback<string[]>>(arg);
			ares_txt_reply *reply;
			int r = ares_parse_txt_reply(abuf, alen, out reply);
			if (r != 0) {
				cb.End(Ensure.Exception(r), null);
			} else {
				int n = length(reply);
				int j = 0;
				string[] res = new string[n];
				for (ares_txt_reply *i = reply; i != null; i = i->next) {
					res[j] = Marshal.PtrToStringAnsi(i->txt, (int)i->length);
					j++;
				}
				cb.End(null, res);
			}
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
