using System.Web.UI;
using Microsoft.Practices.Unity.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
   // [AuthorizeClaim(Resource = SecuredResources.ProjectTemplateAdministration, Action = SecuredActions.View)]
    public class TemplateAdminBaseController : BaseController
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TemplateAdminController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
        public TemplateAdminBaseController(IUserContext userContext, ILogHelper logHelper, IPortalConfiguration portalConfiguration,
            ITempDataProviderFactory tempDataProviderFactory)
            : base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
        {
           
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


        /// <summary>
        /// Called before the action result that is returned by an action method is executed.
        /// </summary>
        /// <param name="filterContext">Information about the current request and action result</param>
        protected override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            var view = filterContext.Result as ViewResultBase;
            SetCommonViewModelProperties(view);
            base.OnResultExecuting(filterContext);
        }

        private void SetCommonViewModelProperties(ViewResultBase view)
        {
            if (view == null) return;

            var model = view.Model as FlexAdminBaseViewModel;
            if (model != null)
            {
                if (!model.PageLinks.Any())
                {
                    model.PageLinks = ActionsLeft(model.EntityType.ToString());
                }

                if (!model.Breadcrumbs.Any())
                {
                    // Split out camel case enum names and pluralize by appending 's'.
                    var entityName = String.Format("{0}s", model.EntityType.GetDisplayName());
                    model.Breadcrumbs = Breadcrumbs(entityName, true);
                }

                if (!model.PageActions.Any())
                {
                    model.PageActions = ActionsRight(model.EntityType);
                }
            }
        }

        internal IEnumerable<Breadcrumb> Breadcrumbs(string crumbText, bool include = false)
        {

            var crumbs = new List<Breadcrumb>();

            if (include)
            {
                crumbs.Add(new Breadcrumb() { Text = "Flex Admin", Url = Url.PageTemplateAdmin() });
            }
            if (crumbText != null)
            {
                crumbs.Add(new Breadcrumb { Text = crumbText, Url = Url.Action(null) });
                ViewBag.SearchTitle = crumbText.Pluralize();
            }

            SetPageMetadata("Flex Admin", crumbs, trackingTitles: new Pair<string, string>("Task Category", "Project Template"));

            return ViewBag.BreadCrumbs;
        }

        internal List<TaxonomyMenuItem> ActionsLeft(string selectedkey, int? projectTemplateCount = null, int? taskTemplateCount = null, int? taskTypeCount = null, int? businessUnitCount = null, int? linksCount = null, int? documentCount = null)
        {
            return new List<TaxonomyMenuItem> {
				new TaxonomyMenuItem { Key = "FlexAdmin",  Text = "Overview", Url = Url.PageTemplateAdmin(), Selected = (selectedkey== "FlexAdmin") , IsRefinable = false, LinkData = {{"class", "empty"}} },
				new TaxonomyMenuItem { Key = EntityType.ProjectTemplate.ToString(),  Text = "Project Templates",Selected = (selectedkey== EntityType.ProjectTemplate.ToString()), Url = Url.PageSearchProjectTemplates(), IsRefinable = false, Count = projectTemplateCount.GetValueOrDefault(), LinkData = {{"class", "empty"}} },
                new TaxonomyMenuItem { Key = EntityType.TaskType.ToString(), Text = "Predefined Tasks", Url = Url.PageSearchTaskTypes() ,Selected = selectedkey== EntityType.TaskType.ToString(), IsRefinable = false, Count = taskTypeCount.GetValueOrDefault() },
				new TaxonomyMenuItem { Key = EntityType.BusinessUnit.ToString() ,  Text = "Business Units", Url = Url.PageSearchBusinessUnits(),Selected = selectedkey== EntityType.BusinessUnit.ToString(), IsRefinable = false, Count =  businessUnitCount.GetValueOrDefault()},
				new TaxonomyMenuItem { Key = EntityType.Link.ToString() ,  Text = "Links", Url = Url.PageSearchLinks(), IsRefinable = false,Selected = selectedkey== EntityType.Link.ToString(), Count =  linksCount.GetValueOrDefault()},
				new TaxonomyMenuItem { Key = EntityType.DocumentTemplate.ToString() ,  Text = "Document Templates", Url = Url.PageSearchDocumentTemplates(), Selected = selectedkey== EntityType.DocumentTemplate.ToString(), IsRefinable = false, Count =  documentCount.GetValueOrDefault()},
			};
        }

        internal List<TaxonomyMenuItem> ActionsRight(EntityType entityType)
        {
		

            var list = new List<TaxonomyMenuItem>();

	        if (!_userContext.CanActOnProjectTemplate())
		        return list;

            if (entityType == EntityType.ProjectTemplate )
            {
                list.Add(new TaxonomyMenuItem
                {
                    Key = "createProjectTemplate",
                    Text = "Create Project Template",
                    Url = Url.PageCreateProjectTemplate(),
                    Modal = true,
                    CssClass = "primary arrow"
                });
            }

			if (entityType == EntityType.TaskCategory)
			{
				list.Add(new TaxonomyMenuItem
				{
					Key = "createTaskCategory",
					Text = "Create Task Category",
					Url = Url.PageCreateTaskTemplate(),
					Modal = true,
					CssClass = "primary arrow"
				});
			}

            if (entityType == EntityType.TaskType)
            {
                list.Add(new TaxonomyMenuItem
                {
                    Key = "createTaskType",
                    Text = "Create Predefined Task",
                    Url = Url.PageCreateTaskType(),
                    Modal = true,
                    CssClass = "primary arrow"
                });
            }

            if (entityType == EntityType.BusinessUnit)
            {
                list.Add(new TaxonomyMenuItem
                {
                    Key = "createBusinessUnit",
                    Text = "Create Business Unit",
                    Url = Url.PageCreateBusinessUnit(),
                    Modal = true,
                    CssClass = "primary arrow"
                });
            }


			if (entityType == EntityType.DocumentTemplate)
			{
				list.Add(new TaxonomyMenuItem
				{
					Key = "createDocumentTemplate",
					Text = "Create Document Template",
					Url = Url.PageCreateDocumentTemplate(),
					Modal = true,
					CssClass = "primary arrow"
				});
			}

            if (entityType == EntityType.Link)
            {
                list.Add(new TaxonomyMenuItem
                {
                    Key = "createLink",
                    Text = "Create Link",
                    Url = Url.PageCreateLink(),
                    Modal = true,
                    CssClass = "primary arrow"
                });
            }

			list.Add(new TaxonomyMenuItem
			{
				Key = "addToFavorites",
				Text = "Add To Favorites",
				Url = "#",
				Modal = true,
				CssClass = "arrow",
				LinkData = { { "DataUrl", Url.PageCreateFavorite() } }
			});

            return list;
        }

    }
}
