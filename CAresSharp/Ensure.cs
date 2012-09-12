using System;
using System.Runtime.InteropServices;

namespace CAresSharp
{
	class Ensure
	{
		[DllImport("cares")]
		unsafe static extern sbyte* ares_strerror(int code);
		
		public unsafe static string StringError(int code)
		{
			return new string(ares_strerror(code));
		}
		
		public static Exception Exception(int code)
		{
			return new Exception(string.Format("{0}({1})", StringError(code), code));
		}
		
		public static void Success(int code)
		{
			if (code != 0) {
				throw Ensure.Exception(code);	
			}
		}
	}
}

