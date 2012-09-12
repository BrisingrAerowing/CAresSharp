using System;

namespace CAresSharp
{
	public class MailExchange
	{
		public MailExchange(string host, int priority)
		{
			Host = host;
			Priority = priority;
		}

		public string Host { get; protected set; }
		public int Priority { get; protected set; }
	}
}

