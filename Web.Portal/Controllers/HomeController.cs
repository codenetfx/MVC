using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web.Mvc;
using UL.Aria.Common.Authorization;
using UL.Enterprise.Foundation.Logging;
using UL.Aria.Web.Common.Claims;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Models.Search;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common;
using Microsoft.AspNet.SignalR;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Logging;
using Logging = UL.Aria.Web.Portal.Logging;
using UL.Aria.Web.Common.Providers;

namespace UL.Aria.Web.Portal.Controllers
{
	/// <summary>
	/// Home controller class.
	/// </summary>
	[System.Web.Mvc.Authorize]
	public class HomeController : BaseController
	{
		private readonly IIncomingOrderProvider _incomingOrderProvider;
		private readonly ISearchProvider _searchProvider;
		private readonly ICompanyProvider _companyProvider;
		private readonly IProfileProvider _profileProvider;
		private readonly ISessionProvider _sessionProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="HomeController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper</param>
        /// <param name="searchProvider">The search provider.</param>
        /// <param name="companyProvider">The company provider.</param>
        /// <param name="incomingOrderProvider">The incoming order provider.</param>
        /// <param name="profileProvider">The profile provider.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="sessionProvider">The session provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>

		public HomeController(IUserContext userContext, ILogHelper logHelper, ISearchProvider searchProvider, ICompanyProvider companyProvider, 
            IIncomingOrderProvider incomingOrderProvider, IProfileProvider profileProvider, IPortalConfiguration portalConfiguration,
            ISessionProvider sessionProvider, ITempDataProviderFactory tempDataProviderFactory)
			: base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
		{
			_incomingOrderProvider = incomingOrderProvider;
			_searchProvider = searchProvider;
			_companyProvider = companyProvider;
			_profileProvider = profileProvider;
			_sessionProvider = sessionProvider;
		}

		/// <summary>
		/// Pages the links.
		/// </summary>
		/// <returns>Returns JsonResult Containing the user permission specific menu items.</returns>
		public JsonResult PageLinks()
		{
			return Json(BuildPageLinks(_userContext, Url), JsonRequestBehavior.AllowGet);
		}


		internal static List<TaxonomyMenuItem> BuildPageLinks(IUserContext userContext, UrlHelper url, string selectedKey = "Home")
		{
			//
			// dont change the Key - views and other code depend on it
			//
			var searchRefinerUrl = url.PageSearchRefiners();
			string homeKey = "Home";
			var actions = new List<TaxonomyMenuItem> 
            {
                new TaxonomyMenuItem 
                { 
                    Key = homeKey,  
                    Text = "Home",
                    Url = url.PageHome(), 
                    IsRefinable = false, Selected = (selectedKey == homeKey)
                },
				new TaxonomyMenuItem { 
                    Key = EntityType.Project.ToString(), 
                    Text = "Projects", 
                    Url = url.PageSearchProjects(), 
                    IsRefinable = true,
                    Selected = (selectedKey == EntityType.Project.ToString()),
                    DataUrl = searchRefinerUrl
                },
              
			};


			actions.Add(new TaxonomyMenuItem

				{
					Key = EntityType.Product.ToString(),
					Text = "Products",
					Url = url.PageSearchProducts(),
					IsRefinable = true,
					Selected = (selectedKey == EntityType.Product.ToString()),
					DataUrl = searchRefinerUrl
				});

			if (userContext.CanAccessOrders())
			{
				actions.Insert(1, new TaxonomyMenuItem
				{
					Key = EntityType.Order.ToString(),
					Text = "Orders",
					Url = url.PageSearchOrders(),
					IsRefinable = true,
					Selected = (selectedKey == EntityType.Order.ToString()),
					DataUrl = searchRefinerUrl
				});
			}

			if (userContext.CanActOnProject())
			{
				actions.Add(new TaxonomyMenuItem
				{
					Key = EntityType.InboundOrder.ToString(),
					Text = "Requests",
					Url = url.PageOrderQueue(),
					IsRefinable = true,
					Selected = (selectedKey == EntityType.InboundOrder.ToString()),
					DataUrl = url.PageRequestRefiners()
				});
			}

			if (userContext.CanAccessTasks())
			{
				actions.Add(new TaxonomyMenuItem
				{
					Key = EntityType.Task.ToString(),
					Text = "Tasks",
					Url = url.PageSearchTasks(),
					IsRefinable = true,
					Selected = (selectedKey == EntityType.Task.ToString()),
					DataUrl = searchRefinerUrl
				});
			}

			if (userContext.IsPortalAdministrator())
			{
				actions.Add(new TaxonomyMenuItem
				{
					Key = EntityType.Company.ToString(),
					Text = "Companies",
					Url = url.PageCompany(userContext),
					IsRefinable = false,
					Selected = (selectedKey == EntityType.Company.ToString())
				});
			}
			else if (userContext.CompanyAdminList().Any())
			{
				actions.Add(new TaxonomyMenuItem
				{
					Key = "members",
					Text = "Members",
					Url = url.PageCompanyMembers(userContext.CompanyId),
					IsRefinable = false,
				});
			}

			if (userContext.IsTemplateAdmin())
			{

				actions.Add(new TaxonomyMenuItem
				{
					Key = "FlexAdmin",
					Text = "Flex Admin",
					Url = url.PageTemplateAdmin(),
					IsRefinable = false,
					Selected = (selectedKey == "FlexAdmin"),
				});
			}

			if (userContext.IsUlEmployee)
			{
				actions.Add(new TaxonomyMenuItem
				{
					Key = "Dashboard",
					Text = "Dashboards",
					Url = url.PageDashboard(),
					IsRefinable = false,
					Selected = (selectedKey == "Dashboard"),
				});
			}

			return actions;
		}

		private HomeViewModel BuildViewModel()
		{
			List<TaxonomyMenuItem> taskStatusCounts = null;
			var canSeeTasks = _userContext.CanAccessTasks();
			var model = new HomeViewModel(_userContext);
			var pageLinkDict = BuildPageLinks(_userContext, Url).ToDictionary(x => x.Key);

			//
			// get counts for content types in sharepoint via a search
			//
			var contentTypeQuery = new SearchCriteria().ApplyAssetTypeRefiner();

			if (canSeeTasks)
				contentTypeQuery.ApplyTaskRefiners();

			var refiners = _searchProvider.Search<ContainerSearchResult>(contentTypeQuery, _sessionProvider, _userContext);

			List<RefinementItem> contentTypeCounts;

			if (refiners.RefinerResults.TryGetAssetTypeRefiner(out contentTypeCounts))
			{
				contentTypeCounts.ForEach(x =>
				{
					if (pageLinkDict.ContainsKey(x.Name))
						pageLinkDict[x.Name].Count = x.Count;
				});

				if (pageLinkDict.ContainsKey(EntityType.Task.ToString()))
				{
					taskStatusCounts = ProjectController.GetTaskRefiners(refiners, _portalConfiguration, _userContext);
					pageLinkDict[EntityType.Task.ToString()].Children.AddRange(taskStatusCounts);
				}

			}
			else
			{
				var message = string.Format("Home page refiner for counts not found.  Refiners available are '{0}'", string.Join(",", refiners.RefinerResults.Keys));
				_logHelper.Log(Logging.MessageIds.HomeControllerRefinerNotFound, LogCategory.Search, LogPriority.Medium, TraceEventType.Warning, HttpContext, message);
			}

			//
			// get counts for items not available via search (Requests, Companies & Members)
			//
			if (pageLinkDict.ContainsKey(EntityType.InboundOrder.ToString()))
			{
				pageLinkDict[EntityType.InboundOrder.ToString()].Count = (int)_incomingOrderProvider.Search(new SearchCriteria(), _userContext, new HashSet<Guid>())
					.SearchCriteria.Paging.TotalResults;
			}

			if (pageLinkDict.ContainsKey(EntityType.Company.ToString()))
			{
				pageLinkDict[EntityType.Company.ToString()].Count = _companyProvider.FetchAllCount();
			}
			else if (pageLinkDict.ContainsKey("members"))
			{
				var companyUsersModel = _profileProvider.FetchAllByCompanyId(_userContext.CompanyId.GetValueOrDefault(), _userContext);
				pageLinkDict["members"].Count = companyUsersModel.Users.Count();
			}



			model.PageLinks = pageLinkDict.Values.ToList();
			ViewBag.PageLinks = model.PageLinks;
			return model;
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
		/// Indexes this instance.
		/// </summary>
		/// <returns></returns>
		public ActionResult Index()
		{
			return View(BuildViewModel());
		}


        ///// <summary>
        ///// Used for testing ui control development
        ///// </summary>
        ///// <returns></returns>
        //public ActionResult Test()
        //{
           
        //    return View();
        //}
	}
}
