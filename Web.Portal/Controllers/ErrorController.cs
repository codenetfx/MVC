using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using AutoMapper;
using UL.Enterprise.Foundation.Logging;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Mvc;

namespace UL.Aria.Web.Portal.Controllers
{
	/// <summary>
	/// Class ErrorController
	/// </summary>
	public class ErrorController : BaseController
	{
        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
		public ErrorController(IUserContext userContext, ILogHelper logHelper, IPortalConfiguration portalConfiguration, 
            ITempDataProviderFactory tempDataProviderFactory)
			: base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
		{
		}

		private ErrorModel BuildViewModel()
		{
			var model = new ErrorModel(_userContext);

			model.PageLinks = HomeController.BuildPageLinks(_userContext, Url, string.Empty);

			return model;
		}

		/// <summary>
		/// Custom error page
		/// </summary>
		/// <returns>ActionResult.</returns>
		public ActionResult Index()
		{
			return View(BuildViewModel());
		}

		/// <summary>
		/// Custom file not found page
		/// </summary>
		/// <returns>ActionResult.</returns>
		public ActionResult FileNotFound()
		{
			return View(BuildViewModel());
		}

		/// <summary>
		/// Unauthorizeds this instance.
		/// </summary>
		/// <returns></returns>
		public ActionResult Unauthorized()
		{
			return View(BuildViewModel());
		}

		/// <summary>
		/// Internals the server.
		/// </summary>
		/// <returns></returns>
		public ActionResult InternalServer()
		{
			return View(BuildViewModel());
		}

		/// <summary>
		/// Tests this instance.
		/// </summary>
		/// <returns>ActionResult.</returns>
		/// <exception cref="System.Exception">Testing</exception>
		public ActionResult Throw(string message = "Test Error")
		{
			throw new Exception(message);
		}

		/// <summary>
		/// Gets the logging category to use for all logging.
		/// </summary>
		/// <value>
		/// The logging category.
		/// </value>
		protected override LogCategory LoggingCategory
		{
			get { return LogCategory.PortalPage; }
		}
	}
}
