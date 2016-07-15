using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Web;
using dotless.Core.Parser.Infrastructure;

using Microsoft.Azure;
using Microsoft.WindowsAzure.ServiceRuntime;

namespace UL.Aria.Web.Portal
{
	/// <summary>
	/// 
	/// </summary>
	[ExcludeFromCodeCoverage]
	public class WebRole : RoleEntryPoint
	{

		/// <summary>
		/// Called when [start].
		/// </summary>
		/// <returns></returns>
		public override bool OnStart()
		{
			try
			{
				var env = CloudConfigurationManager.GetSetting("EnvironmentName");
				System.IO.File.WriteAllText(@"c:\RoleEntryPointlog.txt", "starting Role Entry Point on start." + env);
				var info = new ProcessStartInfo("setup.cmd", env + " Portal") { UseShellExecute = false,  };
				var process = System.Diagnostics.Process.Start(info);
				if (process != null)
				{
					process.WaitForExit();
				}
			}
			catch (Exception exception)
			{
				System.IO.File.WriteAllText(@"c:\RoleEntryPointError.txt", string.Format("{0} \r\n {1} \r\n{2}", exception.Message, exception.StackTrace, exception.Source));
			}
			return base.OnStart();
		}
	}
}