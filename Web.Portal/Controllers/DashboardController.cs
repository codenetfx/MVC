using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Practices.EnterpriseLibrary.Common.Utility;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Enrichment;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Dashboard;
using UL.Aria.Web.Common.Models.Profile;
using UL.Aria.Web.Common.Models.Search;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Mvc.Attributes;
using UL.Aria.Web.Common.Mvc.JqGrid;
using UL.Aria.Web.Common.Providers;
using UL.Enterprise.Foundation.Logging;

namespace UL.Aria.Web.Portal.Controllers
{
	/// <summary>
	/// 
	/// </summary>
	[Authorize]
	public class DashboardController : BaseController
	{
		private readonly ICompanyProvider _companyProvider;
		private readonly IProfileProvider _profileProvider;
		private readonly ISearchProvider _searchProvider;
		private readonly ISessionProvider _sessionProvider;
		private readonly IAdditionalRefinersFactory _additionalRefinersFactory;
		private readonly IDashboardProvider _dashboardProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DashboardController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="companyProvider">The company provider.</param>
        /// <param name="profileProvider">The profile provider.</param>
        /// <param name="searchProvider">The search provider.</param>
        /// <param name="sessionProvider">The session provider.</param>
        /// <param name="additionalRefinersFactory">The additional refiners factory.</param>
        /// <param name="dashboardProvider">The dashboard provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
		public DashboardController(IUserContext userContext, ILogHelper logHelper, IPortalConfiguration portalConfiguration, ICompanyProvider companyProvider, 
            IProfileProvider profileProvider, ISearchProvider searchProvider, ISessionProvider sessionProvider, IAdditionalRefinersFactory additionalRefinersFactory,
            IDashboardProvider dashboardProvider, ITempDataProviderFactory tempDataProviderFactory)
			: base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
		{
			_companyProvider = companyProvider;
			_profileProvider = profileProvider;
			_searchProvider = searchProvider;
			_sessionProvider = sessionProvider;
			_additionalRefinersFactory = additionalRefinersFactory;
			_dashboardProvider = dashboardProvider;
		}


		/// <summary>
		/// Indexes this instance.
		/// </summary>
		/// <returns></returns>
		[ULEmployee]
		public ActionResult Index(SearchCriteria criteria, bool useDefaultSearch = false)
		{

			criteria = UseDefaultSearchCriteria(criteria, EntityType.Dashboard, useDefaultSearch) ??
			           new SearchCriteria().ApplyDashboardSearch();


			var jsonSearchCriteria = _searchProvider.MapToJsonSearchCriteria(criteria);
			jsonSearchCriteria
				.AddDashboardDueDatePickerRefinerDisplay()
				.AddDashboardHandlerAutoCompleteRefinerDisplay(Url);

			ViewBag.JsonCriteria = jsonSearchCriteria;

			ViewBag.JsonProjectCriteria = _dashboardProvider.MapDashboardCriteriaToProjectCriteria(criteria); //MapDashboardCriteraToProjectCriteria(jsonSearchCriteria);
			ViewBag.JsonTaskCriteria = _dashboardProvider.MapDashboardCriteriaToTaskCriteria(criteria);


			var defaultSearchCriteria = new SearchCriteria
			{
				EntityType = EntityType.Dashboard,
				Paging = new Paging { Page = 1, PageSize = 1 }
			}.ApplyDashboardSearch();
			
			var jsonCriterTemp = _searchProvider.MapToJsonSearchCriteria(defaultSearchCriteria);
			jsonCriterTemp
				.AddDashboardDueDatePickerRefinerDisplay()
				.AddDashboardHandlerAutoCompleteRefinerDisplay(Url);
				

			ViewBag.JsonDefaultCriteria = jsonCriterTemp;
			var model = new SearchResultSet<ProjectSearchResult>(_userContext) { SearchCriteria = criteria };
			ViewBag.PageTitle = "Dashboards";
			model.Breadcrumbs = Bredcrumbs();
			model.PageLinks = PageLinks();
			ViewBag.PageLinks = model.PageLinks;
			SetAvailableDefaults(model);
			SetAppliedFilters(criteria, model);
			
		    ViewBag.TopActions = DashboardActions();

			return View(model);
		}

		private IEnumerable<TaxonomyMenuItem> PageLinks()
		{
			return new List<TaxonomyMenuItem>()
			{
				 new TaxonomyMenuItem 
                { 
                    Key = "Home",  
                    Text = "Home",
                    Url = Url.PageHome(), 
                    IsRefinable = false, Selected = false
                },
				new TaxonomyMenuItem(){  
					Key = EntityType.Dashboard.ToString(), 
					Text = "Dashboard", 
					Url = Url.PageDashboard(), 
					IsRefinable = true,
					Selected = true,
					DataUrl = Url.Action("Refiners")
				}
			};
		}

		/// <summary>
		/// Gets the project list.
		/// </summary>
		/// <param name="criteria">The criteria.</param>
		/// <param name="postData">The post data.</param>
		/// <returns></returns>
		[ULEmployee]
		public ActionResult GetProjectList(JsonSearchCriteria criteria, JqGridPostData postData)
		{
			var spcriteria = _searchProvider.MapSearchCriteria(criteria);

			if (spcriteria == null)
			{
				spcriteria = new SearchCriteria();
				spcriteria.ApplyDashboardProjectSearch();
			}
			spcriteria.EntityType = EntityType.Project;

			CleanFilterSubscripts(spcriteria);
			MapJqGridDataToSearchCriteria(spcriteria, postData);
			var model = Search<ProjectSearchResult>(spcriteria);
			var facadeList = (from obj in model.Results
							  select new DashboardProjectRowFacade
							  {
								  Id = obj.ProjectId,
								  CompanyId = obj.ContainerId,
								  CompanyName = obj.CompanyName,
                                  EndDate = obj.EndDate.ToDateString(),
								  EndDateRaw = obj.EndDate,
								  OrderNumber = obj.OrderNumber,
								  ProjectHandler = obj.ProjectHandler,
								  ProjectName = obj.ProjectName,
								  ProjectNameUrl = Url.PageProjectDetails(obj.ProjectId),
								  ProjectStatus = obj.ProjectStatus,
							  }).ToList();

			return Json((new JqGridResults<DashboardProjectRowFacade>
			{
				CurrentPage = spcriteria.Paging.Page,
				CustomDataObject = spcriteria,
				Id = Guid.NewGuid(),
				RepeatItems = true,
				TotalPages = model.SearchCriteria.Paging.LastPageNumber,
				TotalRecords = model.SearchCriteria.Paging.TotalResults,
				Results = facadeList
			}).ToJqGridResult(), JsonRequestBehavior.AllowGet);
		}


		/// <summary>
		/// Gets the task list.
		/// </summary>
		/// <param name="criteria">The criteria.</param>
		/// <param name="postData">The post data.</param>
		/// <returns></returns>
		[ULEmployee]
		public ActionResult GetTaskList(JsonSearchCriteria criteria, JqGridPostData postData)
		{
			var spcriteria = _searchProvider.MapSearchCriteria(criteria);

			if (spcriteria == null)
			{
				spcriteria = new SearchCriteria();
				spcriteria.ApplyTaskSearch(false);
			}
			spcriteria.EntityType = EntityType.Task;
		 	CleanFilterSubscripts(spcriteria);
			MapJqGridDataToSearchCriteria(spcriteria, postData);


			var model = Search<TaskSearchResult>(spcriteria);

			EnrichResultsWithCompanyName(model);
			var facadeList = (from obj in model.Results
							  select new DashboardTaskRowFacade
							  {
								  Id = obj.Id,
								  CompanyId = obj.CompanyId,
								  CompanyName = obj.CompanyName,
                                  EndDate = obj.DueDate.ToDateString(),
								  EndDateRaw = obj.DueDate,
								  TaskName = obj.Name,
								  ProjectId = obj.ProjectId,
								  ProjectName = obj.ProjectName,
								  TaskOwner = obj.TaskOwner,
								  OrderNumber = obj.OrderNumber,
								  TaskStatus = obj.Status,
								  ProjectNameUrl = Url.PageProjectDetails(obj.ProjectId),
                                  TaskNameUrl = Url.PageTaskDetails(obj.Id),
                                  StartDate = obj.StartDate.ToDateString(),
                                  ReminderDate = obj.ReminderDate.ToDateString(),
                              }
							).ToList();
			return Json((new JqGridResults<DashboardTaskRowFacade>
			{
				CurrentPage = spcriteria.Paging.Page,
				CustomDataObject = spcriteria,
				Id = Guid.NewGuid(),
				RepeatItems = true,
				TotalPages = model.SearchCriteria.Paging.LastPageNumber,
				TotalRecords = model.SearchCriteria.Paging.TotalResults,
				Results = facadeList
			}).ToJqGridResult(), JsonRequestBehavior.AllowGet);

		}
        
		/// <summary>
		/// Refiners the specified criteria.
		/// </summary>
		/// <param name="criteria">The criteria.</param>
		/// <returns></returns>
		public ActionResult Refiners(JsonSearchCriteria criteria)
		{
			criteria.Paging.PageSize = 1;
			criteria.Paging.Page = 1;
			criteria.EntityType = EntityType.Dashboard;
			criteria.Filters.Where(x => x.Name.StartsWith("$")).ToList().ForEach(x => criteria.Filters.Remove(x));
			var spCriteria = _searchProvider.MapSearchCriteria(criteria);
			var refinerMenu = new List<JsonTaxonomyMenuItem>();
			refinerMenu.AddRange(_additionalRefinersFactory.GetRefiners(spCriteria));
			foreach (var jsonTaxonomyMenuItem in refinerMenu)
			{
				jsonTaxonomyMenuItem.Key = jsonTaxonomyMenuItem.Key.Replace(".", "___");
			}
			return Json(refinerMenu);
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

		internal static void CleanFilterSubscripts(SearchCriteria criteria)
		{
			var filters = criteria.Filters.Where(x => !x.Key.StartsWith("$")).Select(x => new
			{
				Key = x.Key.Substring(0, x.Key.Contains("[") ? x.Key.IndexOf("[", StringComparison.Ordinal) : x.Key.Length)
				,
				x.Value
			}).ToList(); //x.Key.Replace("[0]", "").Replace("[1]", "").Replace("[2]", ""),
			criteria.Filters.Clear();
			foreach (var item in filters)
			{
				if (!criteria.Filters.ContainsKey(item.Key))
				{
					criteria.Filters.Add(item.Key, item.Value);
				}
				else
				{
					criteria.Filters[item.Key].Add(item.Value.FirstOrDefault());
				}
			}
		}

		private void MapJqGridDataToSearchCriteria(SearchCriteria criteria, JqGridPostData postData)
		{
			// TODO JML need to fix JSON as we get an array back, but need a dictionary.
			// suspect multiple refiners for same field would each come in with some index w same name.
			var filters = criteria.Filters.ToDictionary(x => x.Key.Replace("[0]", ""), y => y.Value);
			criteria.Filters.Clear();
			criteria.Filters = filters;

			criteria.Paging.Page = postData.page;
			criteria.Paging.PageSize = postData.rows;
			//criteria.SortOrder = (postData.sord == "asc")
			//	? SortDirectionDto.Ascending
			//	: SortDirectionDto.Descending;
			//criteria.SortField = postData.sidx;


		}
		/// <summary>
		/// Enriches the name of the results with company.
		/// This process uses a dictionary so that repeated ids are not fetched more than onces from the data source.
		/// </summary>
		/// <param name="model">The model.</param>
		internal void EnrichResultsWithCompanyName(SearchResultSet<TaskSearchResult> model)
		{
			var companyLookup = new Dictionary<Guid, string>();

			foreach (var item in model.Results)
			{
				Guid companyId = item.CompanyId.GetValueOrDefault(Guid.Empty);
				if (companyId != Guid.Empty)
				{
					if (!companyLookup.ContainsKey(companyId))
					{
						companyLookup.Add(companyId, _companyProvider.FetchDisplayNameById(companyId));
					}
					item.CompanyName = companyLookup[companyId];
				}
			}
		}

		private SearchResultSet<TResult> Search<TResult>(SearchCriteria searchCriteria)
			where TResult : SearchResult
		{
			var results = _searchProvider.Search<TResult>(searchCriteria, _sessionProvider, _userContext);
			return results;
		}

		private IEnumerable<Breadcrumb> Bredcrumbs()
		{
			var bredcrumbs = new List<Breadcrumb>()
			{
				new Breadcrumb() { Text = "Home", Url = Url.PageHome()},
				new Breadcrumb() {Text = "Dashboards", Url =  Url.Action(null)}

			};

			return bredcrumbs;
		}

		private void SetAppliedFilters(SearchCriteria criteria, SearchResultSet<ProjectSearchResult> resultsSet)
		{
			foreach (var filter in criteria.Filters)
			{
				var tmi = new TaxonomyMenuItem
				{
					Key = filter.Key,
					Count = filter.Value.Count,
					Text = filter.Key,
				};
				filter.Value.ForEach(x =>
					tmi.AddChild(new TaxonomyMenuItem
					{
						Key = x,
						Count = 0,
						Text = x,
						RefinementValue = x
					})
					);
				resultsSet.AppliedFilters.Add(tmi);
			}
			resultsSet.AppliedFilters.ForEach(x => _searchProvider.FormatRefiner(x, _userContext));
		}

		private SearchCriteria UseDefaultSearchCriteria(SearchCriteria criteria, EntityType entityType, bool useDefaultSearch)
		{
			var setSearchCriteriaAction = new Func<SearchCriteria>(() =>
			{
				if (useDefaultSearch)
				{
					var favDefualtCritera = GetDefaultCriteriaIfApplicable(true, _profileProvider);
					if (favDefualtCritera != null)
					{
						criteria = favDefualtCritera;
						return criteria;
					}
				}
				criteria.ApplyDashboardSearch();

				return criteria;
			});
			criteria = SearchCriteriaExtensions.SetSearchCriteriaFromTempIfNeeded(entityType, setSearchCriteriaAction, TempData);
			return criteria ?? new SearchCriteria();
		}

		private void SetAvailableDefaults(SearchResultSet<ProjectSearchResult> model)
		{
			var controller = ControllerContext.RouteData.Values["controller"] as string;
			var action = ControllerContext.RouteData.Values["action"] as string;
            var location = @"/" + controller + (action != null && action.ToLowerInvariant() != "index" ? @"/" + action : string.Empty);

			var availableDefaults = _profileProvider.FetchAvailableDefaultsByLocation(location, _userContext);
			var searchFavorites = availableDefaults as SearchFavorite[] ?? availableDefaults.ToArray();
			model.ActiveDefault = searchFavorites.FirstOrDefault(x => x.ActiveDefault); ;
			model.AvailableDefaults = searchFavorites;
			model.ShouldShowFilters = true;
		}

        private List<TaxonomyMenuItem> DashboardActions()
        {
            var actionsRight = new List<TaxonomyMenuItem>
	        {
	            new TaxonomyMenuItem
	            {
	                Key = "manage",
	                Text = "Manage My Team",
	                Url = Url.PageEditUserTeam(),
	                Modal = true,
	                ModalWidth = 430,
	                CssClass = "arrow"
	            }
	        };
	
            return actionsRight;
        }
	}
}
