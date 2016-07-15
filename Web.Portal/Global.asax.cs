using System;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens;
using System.Linq;
using System.Security.Claims;
using System.ServiceModel.Channels;
using System.Web;
using System.Web.Mvc;
using Newtonsoft.Json.Converters;
using UL.Aria.Common.Authorization;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Claims;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Providers;
using System.Web.Helpers;
using System.Web.Routing;
using System.Web.Optimization;
using Microsoft.Practices.Unity;
using UL.Aria.Web.Common.Unity;
using UL.Aria.Web.Common.Logging;
using System.Diagnostics;
using System.Threading;
using System.IdentityModel.Services;
using UL.Aria.Web.Common.ValueProviders;
using UL.Enterprise.Foundation.Logging;
using ClaimTypes = System.IdentityModel.Claims.ClaimTypes;

namespace UL.Aria.Web.Portal
{
	/// <summary>
	/// MVC application class.
	/// </summary>
	[ExcludeFromCodeCoverage]
	public class AriaApplication : HttpApplication
	{
		private static RequestDependencyResolver _objectFactory;

		/// <summary>
		/// Initializes a new instance of the <see cref="AriaApplication"/> class.
		/// </summary>
		public AriaApplication() { }

		/// <summary>
		/// The Application_Start method is called only one time during the life cycle of an application. 
		/// Called when the first resource (such as a page) in an ASP.NET application is requested. 
		/// You can use this method to perform startup tasks such as loading data into the cache and 
		/// initializing static values.
		/// You should set only static data during application start. Do not set any instance data because 
		/// it will be available only to the first instance of the HttpApplication class that is created.
		/// </summary>
		protected void Application_Start(object sender, EventArgs e)
		{
			AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;

			//
			// set up our DI
			//
			var unity = new UnityContainer();
			UnityBootstrapper.RegisterTypes(unity);

			//
			// create our per-request resolver and tell MVC to use it
			//
			_objectFactory = new RequestDependencyResolver(unity);
			DependencyResolver.SetResolver(_objectFactory);

			//
			// Register all MVC routes, filters, bundles and signalR hubs
			//
			RouteTable.Routes.MapHubs();
			AreaRegistration.RegisterAllAreas();
			FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
			RouteConfig.RegisterRoutes(RouteTable.Routes);
			BundleConfig.RegisterBundles(BundleTable.Bundles);
			BindingConfig.RegisterGlobalBindings(ModelBinders.Binders);
			FederatedAuthentication.FederationConfigurationCreated += FederatedAuthentication_FederationConfigurationCreated;
			ValueProviderFactories.Factories.Remove(ValueProviderFactories.Factories.OfType<JsonValueProviderFactory>().FirstOrDefault());
			ValueProviderFactories.Factories.Add(new JsonDotNetValueProviderFactory());
			
		}

		void FederatedAuthentication_FederationConfigurationCreated(object sender, System.IdentityModel.Services.Configuration.FederationConfigurationCreatedEventArgs e)
		{
			e.FederationConfiguration.IdentityConfiguration.ClaimsAuthenticationManager = new PortalClaimsAuthenticationManager(
				_objectFactory.GetService<IProfileProvider>(),
				_objectFactory.GetService<ILogHelper>(),
				_objectFactory.GetService<ICompanyProvider>(),
				_objectFactory.GetService<IPortalConfiguration>(),
				_objectFactory.GetService<IUserBusinessClaimFactory>(),
				_objectFactory.GetService<ITermsAndConditionsProvider>(),
				_objectFactory.GetService<IUserContext>());
			e.FederationConfiguration.IdentityConfiguration.ClaimsAuthorizationManager = new PortalClaimsAuthorizationManager(_objectFactory.GetService<IAuthorizationManager>());
			
			FederatedAuthentication.WSFederationAuthenticationModule.AuthorizationFailed +=
				WSFederationAuthenticationModule_AuthorizationFailed;
			FederatedAuthentication.WSFederationAuthenticationModule.SignInError += WSFederationAuthenticationModule_SignInError;

		}

		///// <summary>
		///// Called once per lifetime of the application before the application is unloaded.
		///// </summary>
		///// <param name="sender">The source of the event.</param>
		///// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		//protected void Application_End(object sender, EventArgs e)
		//{
		//}

		/// <summary>
		/// Handles the AuthorizeRequest event of the Application control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		/// <exception cref="System.UnauthorizedAccessException"></exception>
		protected void Application_AuthorizeRequest(object sender, EventArgs e)
		{
			var app = (HttpApplication)sender;

			var authorizationManager = _objectFactory.GetService<IAuthorizationManager>();
			var claimsPrincipal = Thread.CurrentPrincipal as ClaimsPrincipal;

			AuthorizeUser(authorizationManager, claimsPrincipal, app.Context);
			VerifyLegalTerms(claimsPrincipal, app.Context);
		}

		/// <summary>
		/// Authorizes the user.
		/// </summary>
		/// <param name="authorizationManager">The authorization manager.</param>
		/// <param name="claimsPrincipal">The claims principal.</param>
		/// <param name="context">The context.</param>
		/// <exception cref="System.Web.HttpException">Not authorized to access this resource</exception>
		protected void AuthorizeUser(IAuthorizationManager authorizationManager, ClaimsPrincipal claimsPrincipal, HttpContext context)
		{
			if (claimsPrincipal.Identity.IsAuthenticated)
			{
				var canAccessOneCompany = authorizationManager.Authorize(claimsPrincipal, SecuredResources.CompanyInstance, SecuredActions.View, null);
				if (!canAccessOneCompany)
				{
					context.RedirectUnauthorized();
				}
			}
		}

		/// <summary>
		/// Verifies no legal terms need to be accepted, and redirects if some do.
		/// </summary>
		/// <param name="claimsPrincipal">The claims principal.</param>
		/// <param name="context">The context.</param>
		protected void VerifyLegalTerms(ClaimsPrincipal claimsPrincipal, HttpContext context)
		{
			var mustAcceptTerm = claimsPrincipal.FindFirst(SecuredClaims.RequireLegalAcceptance);
			if (mustAcceptTerm != null)
			{
				context.RedirectTermsAndConditions(Guid.Parse(mustAcceptTerm.Value));
			}
		}


		/// <summary>
		/// Handles the BeginRequest event of the AriaApplication control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		protected void Application_BeginRequest(object sender, EventArgs e)
		{
			Trace.CorrelationManager.ActivityId = Guid.NewGuid();
		}

		/// <summary>
		/// Handles the EndRequest event of the AriaApplication control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		protected void Application_EndRequest(object sender, EventArgs e)
		{
			//
			// Explicitally dispose of our per-request child container to help out GC
			//
			RequestDependencyResolver.DisposeOfChildContainer();
		}

		///// <summary>
		///// Handles the Error event of the AriaApplication control.
		///// </summary>
		///// <param name="sender">The source of the event.</param>
		///// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		///// <exception cref="System.NotImplementedException"></exception>
		//protected void Application_Error(object sender, EventArgs e)
		//{
		//	//HttpApplication app = (HttpApplication)sender;
		//	//var ex = app.Server.GetLastError();
		//	//IMPORTANT: if we do a Server.ClearError() here, we must log the error otherwise LogRequest below wont
		//}

		/// <summary>
		/// Handles the Application_AcquireRequestState event.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		protected void Application_AcquireRequestState(object sender, EventArgs e)
		{
			HttpApplication app = (HttpApplication)sender;

			if (app.Context.Session != null && app.Context.User.Identity.IsAuthenticated)
			{
				//
				// if we have not initialized the stash yet for this user's session
				//
				if (SessionStash.Retrieve(app.Context.Session) == null)
				{
					var profileProvider = _objectFactory.GetService<IProfileProvider>();
					var sessionProvider = _objectFactory.GetService<ISessionProvider>();
					var userContext = _objectFactory.GetService<IUserContext>();
					var userProfile = profileProvider.FetchByUserName(app.Context.User.Identity.Name);

					if (userProfile != null)
					{
						profileProvider.UpdateSessionStash(new HttpSessionStateWrapper(app.Context.Session), sessionProvider, userContext);
					}
				}
			}
		}

		/// <summary>
		/// Handles the LogRequest event of the AriaApplication control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		/// <exception cref="System.NotImplementedException"></exception>
		protected void Application_LogRequest(object sender, EventArgs e)
		{
			var app = (HttpApplication)sender;
			var log = _objectFactory.GetService<ILogHelper>();

			LogErrorOrRequest(app, log);
		}

		/// <summary>
		/// Logs the request.
		/// </summary>
		/// <param name="app">The application.</param>
		/// <param name="log">The log.</param>
		protected void LogErrorOrRequest(HttpApplication app, ILogHelper log)
		{
			var ex = app.Server.GetLastError();

			if (ex != null && (ex as SecurityTokenException) != null)
				log.LogError(app.Context, new Exception("Certificate error. Please check Certificate and its thumbprint.", ex));
			else if (ex != null)
			{
				log.LogError(app.Context, ex);
			}
			else
				log.LogRequest(app.Context);
		}


		/// <summary>
		/// Event raised after the user is signed in.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		protected void WSFederationAuthenticationModule_SignedIn(object sender, EventArgs e)
		{
			//WSFederationAuthenticationModule authModule = (WSFederationAuthenticationModule)sender;
			var log = _objectFactory.GetService<ILogHelper>();

			log.LogLogin(HttpContext.Current);
		}

		/// <summary>
		/// Handles the AuthorizationFailed event of the WSFederationAuthenticationModule control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		protected void WSFederationAuthenticationModule_AuthorizationFailed(object sender, AuthorizationFailedEventArgs e)
		{
			var log = _objectFactory.GetService<ILogHelper>();
			log.Log(10004, LogCategory.Authentication, LogPriority.High, TraceEventType.Error, new HttpContextWrapper(HttpContext.Current), "Error while getting security token. Please check IAM certificate and thumb print");
			HttpContext.Current.RedirectUnauthorized();
		}


		/// <summary>
		/// Handles the SignInError event of the WSFederationAuthenticationModule control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="ErrorEventArgs"/> instance containing the event data.</param>
		protected void WSFederationAuthenticationModule_SignInError(object sender, ErrorEventArgs e)
		{
			var log = _objectFactory.GetService<ILogHelper>();
			log.Log(10005, LogCategory.Authentication, LogPriority.High, TraceEventType.Error, new HttpContextWrapper(HttpContext.Current), "Error while getting security token. Please check IAM certificate and thumb print");
			HttpContext.Current.RedirectUnauthorized();

		}
		/// <summary>
		/// Handles the SessionSecurityTokenCreated event of the WSFederationAuthenticationModule control so that we can customize
		/// the duration of our session cookie.  This occurs immediately before WSFederationAuthenticationModule_SignedIn. 
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
		protected void WSFederationAuthenticationModule_SessionSecurityTokenCreated(object sender, SessionSecurityTokenCreatedEventArgs e)
		{
			var config = _objectFactory.GetService<IPortalConfiguration>();

			if (config.ShouldOverrideStsSessionTime)
			{
				var authModule = (WSFederationAuthenticationModule)sender;
				var duration = authModule.FederationConfiguration.CookieHandler.PersistentSessionLifetime.GetValueOrDefault(TimeSpan.FromMinutes(config.OverrideStsSessionTimeout));
				var validTo = e.SessionToken.ValidFrom.Add(duration);
				const bool persistant = false; //will make cookie (not auth) expire when browser is closed

				//
				// rebuild session security token using our custom duration and replace the one provided bt the STS
				//
				var token = FederatedAuthentication.SessionAuthenticationModule.CreateSessionSecurityToken(e.SessionToken.ClaimsPrincipal,
						e.SessionToken.Context, e.SessionToken.ValidFrom, validTo, persistant);

				e.SessionToken = token;
			}
		}

	}
}