using System;
using System.Runtime.InteropServices;

namespace CAresSharp
{
	delegate void sock_state_cb_delegate(IntPtr data, int socket, int read, int write);
	
	enum ARES_OPT
	{
		SOCK_STATE_CB = (1 << 9),
	}
	
	internal unsafe struct ares_options {
		public int flags;
		public int timeout;
		public int tries;
		public int ndots;
		public ushort udp_port;
		public ushort tcp_port;
		public int socket_send_buffer_size;
		public int socket_receive_buffer_size;
		public IntPtr servers;
		public int nservers;
		public sbyte **domains;
		public int ndomains;
		public sbyte *lookups;
		public sock_state_cb_delegate sock_state_cb;
		public IntPtr sock_state_cb_data;
		public IntPtr sortlist;
		public int nsort;
		public int ednspsz;
	}
	
	class CAresSocketCallback : Callback
	{
		Action<CAresChannel, int, bool, bool> cb;
		CAresChannel channel;

		public CAresSocketCallback(CAresChannel channel, Action<CAresChannel, int, bool, bool> callback)
		{
			cb = callback;
			this.channel = channel;
		}

		public void Invoke(int socket, bool read, bool write)
		{
			if (cb != null) {
				cb(channel, socket, read, write);
			}
		}
	}
	
	public class CAresChannelOptions
	{
		public int Timeout { get; set; }
		public int Tries { get; set; }
		public int UdpPort { get; set; }
		public int TcpPort { get; set; }
		
		
		public Action<CAresChannel, int, bool, bool> SocketCallback { get; set; }
		/*
			set {
				if (value == null) {
					cb = null;
				} else {
					cb = new CAresSocketCallback(value);
					option_mask |= (int)ARES_OPT.SOCK_STATE_CB;
				}
			}
		}
		*/
		
		internal int option_mask;
		
		internal ares_options ToStruct(CAresChannel channel)
		{
			var options = new ares_options() {
				timeout = Timeout,
				tries = Tries,
				udp_port = (ushort)UdpPort,
				tcp_port = (ushort)TcpPort
			};
			
			options.sock_state_cb = sock_state_cb;
			
			if (SocketCallback != null) {
				var cb = new CAresSocketCallback(channel, SocketCallback);
				option_mask |= (int)ARES_OPT.SOCK_STATE_CB;
				options.sock_state_cb_data = cb.Handle;
			}
			
			return options;
		}
		
		static void sock_state_cb(IntPtr data, int socket, int read, int write)
		{
			var cb = Callback.GetObject<CAresSocketCallback>(data);
			cb.Invoke(socket, read != 0, write != 0);
		}
	}
}

