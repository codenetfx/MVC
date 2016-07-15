using System.Web.UI;
using Microsoft.Practices.Unity.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UL.Aria.Common.Authorization;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Search;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Models.TemplateAdmin;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Mvc.Attributes;
using UL.Aria.Web.Common.Providers;
using UL.Enterprise.Foundation;
using UL.Enterprise.Foundation.Logging;

namespace UL.Aria.Web.Portal.Controllers
{
	/// <summary>
	/// Template Admin Controller
	/// </summary>
	[AuthorizeClaim(Resource = SecuredResources.ProjectTemplateAdministration, Action = SecuredActions.View)]
    public class TemplateAdminController : TemplateAdminBaseController
	{
		private readonly IProjectTemplateProvider _projectTemplateProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateAdminController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="projectTemplateProvider">The project template provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
        public TemplateAdminController(IUserContext userContext, ILogHelper logHelper, IPortalConfiguration portalConfiguration,
            IProjectTemplateProvider projectTemplateProvider, ITempDataProviderFactory tempDataProviderFactory)
            : base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
        {
            _projectTemplateProvider = projectTemplateProvider;
        }

		/// <summary>
		/// Indexes this instance.
		/// </summary>
		/// <returns></returns>
		public ActionResult Index()
		{
			var model = new TemplateAdminHome();
			//var projectTemplateCount = (int)_projectTemplateProvider.GetAll().Count;
			model.Breadcrumbs = Breadcrumbs("Flex Admin");
			model.PageLinks = ActionsLeft("FlexAdmin");
			return View(model);
				
		}


		/// <summary>
		/// Projects the templates.
		/// </summary>
		/// <param name="searchCriteria">The criteria.</param>
		/// <returns></returns>
		public ActionResult ProjectTemplates(SearchCriteria searchCriteria)
		{
			if (searchCriteria == null)
				searchCriteria = new SearchCriteria();

			searchCriteria.ApplyProjectTemplateSearch();

			var model = _projectTemplateProvider.Search(searchCriteria, _userContext);
			model.PageLinks = ActionsLeft(EntityType.ProjectTemplate.ToString());
			model.Breadcrumbs = Breadcrumbs("Project Templates", true);
			model.PageActions = ActionsRight(EntityType.ProjectTemplate);
			return View("Search", model);
		}	
	}
}
