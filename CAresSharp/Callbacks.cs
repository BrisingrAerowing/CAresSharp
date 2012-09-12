using System;
using System.Runtime.InteropServices;

namespace CAresSharp
{
		unsafe class Callback : IDisposable
		{
			public IntPtr Handle { get; protected set; }

			public GCHandle GCHandle {
				get {
					return *((GCHandle *)Handle.ToPointer());
				}
				set {
					*((GCHandle *)Handle.ToPointer()) = value;
				}
			}

			public Callback()
			{
				Handle = UV.Alloc(sizeof(GCHandle));
				GCHandle = GCHandle.Alloc(this, GCHandleType.Normal);
			}

			public void Dispose()
			{
				if (Handle != IntPtr.Zero) {
					GCHandle.Free();
					Handle = IntPtr.Zero;
				}
			}

			public static T GetObject<T>(IntPtr arg) where T : class
			{
				if (arg == IntPtr.Zero) {
					return default(T);
				}

				var handle = *((GCHandle *)arg.ToPointer());
				return handle.Target as T;
			}
		}

		class AresCallback<T> : Callback where T : class
		{
			Action<Exception, T> cb;

			public AresCallback(Action<Exception, T> callback)
			{
				cb = callback;
			}

			public void End(Exception exception, T arg1)
			{
				if (cb != null) {
					cb(exception, arg1);
				}
				Dispose();
			}
		}
}

