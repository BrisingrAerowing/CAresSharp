using System;
using System.Runtime.InteropServices;

namespace CAresSharp
{
	public static class CAres
	{
		enum LibraryInit
		{
			None = 0,
			Win32 = 1,
			All = Win32
		}
		
		[DllImport("cares")]
		static extern int ares_library_init(LibraryInit flags);
		
		public static void Init()
		{
			int ret = ares_library_init(LibraryInit.All);
			Ensure.Success(ret);
		}
		
		[DllImport("cares")]
		unsafe static extern sbyte *ares_version(IntPtr ptr);
		
		unsafe public static string Version {
			get {	
				return new string(ares_version(IntPtr.Zero));	
			}
		}
		
		[DllImport("cares")]
		static extern void ares_library_cleanup();
		
		public static void Cleanup()
		{
			ares_library_cleanup();
		}
	}
}

