using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Services;
using System.Web.Mvc;
using System.Web.Security;
using Microsoft.AspNet.SignalR.Hubs;
using UL.Aria.Common.Authorization;
using UL.Enterprise.Foundation.Logging;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Providers;
using System.Web;
using System.Net;

namespace UL.Aria.Web.Portal.Controllers
{
	/// <summary>
	/// Login controller class.
	/// </summary>
	public class LoginController : BaseController
	{
		private readonly IEmailProvider _emailProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="LoginController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="emailProvider">The email provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
		public LoginController(IUserContext userContext, ILogHelper logHelper, IPortalConfiguration portalConfiguration, IEmailProvider emailProvider,
            ITempDataProviderFactory tempDataProviderFactory)
			: base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
		{
			_emailProvider = emailProvider;
		}

		/// <summary>
		/// Builds a token and send the user to the configured STS provider.
		/// </summary>
		/// <returns>ActionResult.</returns>
		public ActionResult SignIn(string returnUrl)
		{
			// this will send us back here, therefore, is this method is used, 
			// we must to split SignIn() into a GET and POST method to prevent infinite redirects
			//FederatedAuthentication.WSFederationAuthenticationModule.SignIn("AriaSignIn");

			var isAjax = Request.IsAjaxRequest();
			//ensure we have a return URL, and dont trust the one from an AJAX request
			if (isAjax || string.IsNullOrEmpty(returnUrl))
			{
				if (Request.UrlReferrer != null)
					returnUrl = Request.UrlReferrer.PathAndQuery;
				else
					returnUrl = Url.PageHome();
			}

			//
			// if an AJAX request occurs after auth timeout, we will not be able to redirect 
			// it to the STS auth page (due to inherit cross site scripting limitations)
			// as such, we will return a view that will prompt the user to log in
			//
			if (Request.IsAjaxRequest())
			{
				return PartialView((object)returnUrl);
			}

			// redirect to obtain a token using WS-Federation Passive Protocol.
			FederatedAuthentication.WSFederationAuthenticationModule.RedirectToIdentityProvider("AriaSignIn", returnUrl, false);
			
			return new EmptyResult();
		}

		///// <summary>
		///// Handled the sign in request from the STS portal
		///// </summary>
		///// <param name="returnUrl">The return URL.</param>
		///// <returns>ActionResult.</returns>
		//[HttpGet]
		//public ActionResult SignIn(string returnUrl)
		//{
		//	// WSFAM will only parse the WSFed message and redirect back to the page there, 
		//	// which is Login/?ReturnUrl=<OriginallyRequestedPage> (i.e. this method)
		//	// So let's handle that redirect
		//	if (string.IsNullOrEmpty(returnUrl))
		//		returnUrl = Url.Action("Index", "Home");
		//
		//	return Redirect(returnUrl);
		//}

		/// <summary>
		/// The logout confirmation page after a logout occurs
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public ActionResult SignOut()
		{
		    return View();
		}

		/// <summary>
		/// Logs the users out of both this portal and the STS portal
		/// </summary>
		/// <returns>ActionResult.</returns>
		[HttpPost]
        [ExcludeFromCodeCoverage]
		public ActionResult SignOut(string returnUrl)
		{
			if (string.IsNullOrEmpty(returnUrl))
				returnUrl = Url.Action("SignOut");

			//this logs us out of the STS portal
			//FormsAuthentication.SignOut();

			//this logs us out of Aria portal
			FederatedAuthentication.WSFederationAuthenticationModule.SignOut(returnUrl);
			//FederatedAuthentication.SessionAuthenticationModule.SignOut();
            
			return new EmptyResult();
		}

		/// <summary>
		/// Unauthorized page.
		/// </summary>
		/// <returns></returns>
		public ActionResult Unauthorized()
		{
			if (_userContext.IsUlEmployee)
			{
				ViewBag.Success = false;
				return View(new EmailResponse());
			}

			return View("UnauthorizedNonUL");
		}

		/// <summary>
		/// Unauthorized user requesting access
		/// </summary>
		/// <param name="requestAccess">The request access.</param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult Unauthorized(string requestAccess)
		{
			if(!_userContext.IsUlEmployee)
				throw new HttpException((int)HttpStatusCode.Forbidden, "Not authorized");

			var response = _emailProvider.PortalAccessRequest(_userContext.LoginId);
			ViewBag.Success = !response.Error;

			return View(response);
		}

		/// <summary>
		/// Gets the logging category to use for all logging.
		/// </summary>
		/// <value>
		/// The logging category.
		/// </value>
		protected override LogCategory LoggingCategory
		{
			get { return LogCategory.Authentication; }
		}
	}
}