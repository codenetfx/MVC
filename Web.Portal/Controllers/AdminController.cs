using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using UL.Aria.Common.Authorization;
using UL.Aria.Web.Common;
using UL.Enterprise.Foundation.Logging;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Company;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Mvc.Attributes;
using UL.Aria.Web.Common.Providers;
using UL.Aria.Web.Common.Models.Search;

namespace UL.Aria.Web.Portal.Controllers
{
	/// <summary>
	/// Administration pages for the UL administrator.
	/// </summary>
	[AuthorizeClaim(Resource = SecuredResources.AriaAdministration, Action = SecuredActions.View)]
	public sealed class AdminController : BaseController
	{
		private readonly ICompanyProvider _companyProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdminController" /> class.
        /// </summary>
        /// <param name="companyProvider">The company provider.</param>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
		public AdminController(ICompanyProvider companyProvider, IUserContext userContext, ILogHelper logHelper,
            IPortalConfiguration portalConfiguration, ITempDataProviderFactory tempDataProviderFactory)
			: base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
		{
			_companyProvider = companyProvider;
		}

		/// <summary>
		/// Gets the logging category to use for all logging.
		/// </summary>
		/// <value>
		/// The logging category.
		/// </value>
		protected override LogCategory LoggingCategory
		{
			get { return LogCategory.User; }
		}

		/// <summary>
		/// Default page.
		/// </summary>
		/// <returns>ActionResult.</returns>
		public ActionResult Index()
		{
			return RedirectToAction("Companies");
		}

		/// <summary>
		/// Searches the companies.
		/// </summary>
		/// <param name="searchCriteria">The search criteria.</param>
		/// <returns></returns>
		public ActionResult Companies(SearchCriteria searchCriteria)
		{
            if (searchCriteria == null)
            {
                searchCriteria = new SearchCriteria();
                searchCriteria = searchCriteria.ApplyCompanySearch();
            }

            searchCriteria.EntityType = EntityType.Company;
			var companies = _companyProvider.Search(searchCriteria, _userContext);

			ViewBag.PageLinks = HomeController.BuildPageLinks(_userContext, Url, EntityType.Company.ToString());

			var crumb = new Breadcrumb() { Text = "Companies", Url = Url.PageCompanySearch() };
			SetPageMetadata(crumb.Text, new[] {crumb});

			companies.Breadcrumbs = ViewBag.BreadCrumbs;

			return View(companies);
		}
	}
}
