using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Practices.Unity.Utility;
using UL.Aria.Common;
using UL.Aria.Web.Portal.Logging;
using UL.Enterprise.Foundation.Logging;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Help;
using UL.Aria.Web.Common.Models.Profile;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Providers;

namespace UL.Aria.Web.Portal.Controllers
{
	/// <summary>
	/// Terms and conditions and other legal pages
	/// </summary>
	public class LegalController : BaseController
	{
		private readonly ITermsAndConditionsProvider _termsAndConditionsProvider;
		private readonly IProfileProvider _profileProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="HelpController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="termsAndConditionsProvider">The terms and conditions provider.</param>
        /// <param name="profileProvider">The profile provider.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
		public LegalController(IUserContext userContext, ILogHelper logHelper, ITermsAndConditionsProvider termsAndConditionsProvider,
            IProfileProvider profileProvider, IPortalConfiguration portalConfiguration, ITempDataProviderFactory tempDataProviderFactory)
			: base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
		{
			_termsAndConditionsProvider = termsAndConditionsProvider;
			_profileProvider = profileProvider;
		}

		/// <summary>
		/// Redirects to default Online Policies page (Terms of Use).
		/// </summary>
		/// <returns></returns>
		public ActionResult Index()
		{
			return RedirectToAction("TermsOfUse");
		}

		/// <summary>
		/// Terms the and conditions.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="redirectUrl">The redirect URL.</param>
		/// <returns></returns>
		[HttpGet]
		public ActionResult TermsAndConditions(Guid id, string redirectUrl)
		{
			var vm = _termsAndConditionsProvider.FetchById(id);

			return View(vm);
		}

		/// <summary>
		/// Termses the and conditions.
		/// </summary>
		/// <returns></returns>
		[HttpPost]
		public ActionResult TermsAndConditions(TermsAndConditionsModel vm, string redirectUrl)
		{
			if (ModelState.IsValid)
			{
				if (vm.IsAcknoledged)
				{
					_profileProvider.AcceptTermsAndConditions(vm, _userContext);

					if (string.IsNullOrWhiteSpace(redirectUrl))
						return RedirectToAction("Index", "Home");
					return Redirect(redirectUrl);
				}
			}

			ModelState.AddModelError(string.Empty, "Please acknowledge before accepting.");
			return TermsAndConditions(vm.Id.GetValueOrDefault(), redirectUrl);
		}

		/// <summary>
		/// Declines the terms.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <returns></returns>
		public ActionResult DeclineTerms(Guid id)
		{
			var terms = _termsAndConditionsProvider.FetchById(id) ?? new TermsAndConditionsModel() { Id = id };

			string message = string.Format("User '{0}' has declined the '{1}' version '{2}' Terms & Conditions (ID:{3})", _userContext.LoginId, terms.Type, terms.Version, terms.Id);
			_logHelper.Log(MessageIds.LegalControllerDeclinedTandC, LogCategory.Legal, LogPriority.High, TraceEventType.Information, HttpContext, message);

			return Redirect(Url.PageSignOut());
		}


		/// <summary>
		/// The site's terms of use policy.
		/// </summary>
		/// <returns></returns>
		public ActionResult TermsOfUse()
		{
			var model = new BaseViewModel();

			model.Breadcrumbs = SetPageMetadata("Terms of Use");
			model.PageLinks = OnlinePolicySectionActions();

			//
			// get latest version of company admin terms for display
			//
			var currentVersion = _termsAndConditionsProvider.FetchByType(TermsAndConditionsType.CompanyAdministrator);
			if (currentVersion != null)
			{
				ViewBag.CompanyAdminTerms = currentVersion.LegalText;
			}

			return View(model);
		}

		/// <summary>
		/// The site's cookie policy.
		/// </summary>
		/// <returns></returns>
		public ActionResult AboutCookies()
		{
			var model = new BaseViewModel();

			model.Breadcrumbs = SetPageMetadata("About Cookies");
			model.PageLinks = OnlinePolicySectionActions();

			return View(model);
		}

		internal List<TaxonomyMenuItem> OnlinePolicySectionActions()
		{
			var actions = new List<TaxonomyMenuItem> {
				new TaxonomyMenuItem {Text = "Terms of Use", Url = Url.PageTermsOfUse()},
				new TaxonomyMenuItem {Text = "About Cookies", Url = Url.PageAboutCookies()}
			};

			return actions;
		}

		/// <summary>
		/// Sets the page metadata.
		/// </summary>
		/// <param name="crumbText">The crumb text.</param>
		/// <returns></returns>
		internal IEnumerable<Breadcrumb> SetPageMetadata(string crumbText)
		{
			var list = new List<Breadcrumb>();

			list.Add(new Breadcrumb { Text = "Online Policies", Url = Url.PageOnlinePolicies() });

			if (crumbText != null)
				list.Add(new Breadcrumb() { Text = crumbText, Url = Url.Action(null) });

			SetPageMetadata(crumbText, list);

			return ViewBag.Breadcrumbs;
		}

		/// <summary>
		/// Gets the logging category to use for all logging.
		/// </summary>
		/// <value>
		/// The logging category.
		/// </value>
		protected override LogCategory LoggingCategory
		{
			get { return LogCategory.Legal; }
		}

	}
}
