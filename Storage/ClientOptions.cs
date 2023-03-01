using System;
using System.Collections.Generic;
using System.Text;

namespace Storage
{
	public class ClientOptions
	{
		/// <summary>
		/// The timespan to wait before an HTTP Client request times out.
		/// See: https://learn.microsoft.com/en-us/dotnet/api/system.net.http.httpclient.timeout?view=net-7.0
		/// </summary>
		public TimeSpan HttpClientTimeout = TimeSpan.FromMinutes(5);
	}
}
