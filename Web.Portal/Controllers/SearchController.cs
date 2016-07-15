using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

using AutoMapper;

using Microsoft.Ajax.Utilities;

using UL.Aria.Common.Authorization;
using UL.Aria.Service.Contracts.Dto;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Enrichment;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Profile;
using UL.Aria.Web.Common.Models.Project;
using UL.Aria.Web.Common.Models.Search;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Mvc.Attributes;
using UL.Aria.Web.Common.Mvc.JqGrid;
using UL.Aria.Web.Common.Providers;
using UL.Enterprise.Foundation;
using UL.Enterprise.Foundation.Logging;

using WebGrease.Css.Extensions;

namespace UL.Aria.Web.Portal.Controllers
{
	/// <summary>
	///     Controller class to facilitate searching over Products, Orders, Tests, Documents
	/// </summary>
	[Authorize]
	public sealed class SearchController : BaseController
	{
		private readonly IAdditionalRefinersFactory _additionalRefinersFactory;
		private readonly ICompanyProvider _companyProvider;
		private readonly IIncomingOrderProvider _incomingOrderProvider;
		private readonly IProductFamilyProvider _productFamilyProvider;
		private readonly IProfileProvider _profileProvider;
		private readonly IProjectTemplateProvider _projectTemplateProvider;
		private readonly ISearchProvider _searchProvider;
		private readonly ISessionProvider _sessionProvider;
		private readonly ITaskProvider _taskProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="SearchController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="searchProvider">The search provider.</param>
        /// <param name="productFamilyProvider">The product family provider</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="sessionProvider">The session provider.</param>
        /// <param name="companyProvider">The company provider</param>
        /// <param name="taskProvider">The task provider</param>
        /// <param name="profileProvider">The profile provider.</param>
        /// <param name="incomingOrderProvider">The incoming order provider.</param>
        /// <param name="projectTemplateProvider">The project template provider.</param>
        /// <param name="additionalRefinersFactory">The additional refiner factory.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
		public SearchController(IUserContext userContext, ILogHelper logHelper, ISearchProvider searchProvider,
			IProductFamilyProvider productFamilyProvider, IPortalConfiguration portalConfiguration,
			ISessionProvider sessionProvider, ICompanyProvider companyProvider, ITaskProvider taskProvider,
			IProfileProvider profileProvider, IIncomingOrderProvider incomingOrderProvider,
			IProjectTemplateProvider projectTemplateProvider, IAdditionalRefinersFactory additionalRefinersFactory, 
            ITempDataProviderFactory tempDataProviderFactory)
			: base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
		{
			_searchProvider = searchProvider;
			_productFamilyProvider = productFamilyProvider;
			_sessionProvider = sessionProvider;
			_companyProvider = companyProvider;
			_taskProvider = taskProvider;
			_profileProvider = profileProvider;
			_incomingOrderProvider = incomingOrderProvider;
			_projectTemplateProvider = projectTemplateProvider;
			_additionalRefinersFactory = additionalRefinersFactory;
		}

		/// <summary>
		///     Gets the logging category to use for all logging.
		/// </summary>
		/// <value>
		///     The logging category.
		/// </value>
		protected override LogCategory LoggingCategory
		{
			get { return LogCategory.Search; }
		}

		/// <summary>
		///     Indexes this instance.
		/// </summary>
		/// <returns></returns>
		public ActionResult Index(SearchCriteria criteria)
		{
			criteria = criteria.ApplyGlobalSearchFilter();
			var model = Search<SearchResult>(criteria);

			ViewBag.SearchTitle = "Results";
			ViewBag.CurrentLoginId = _userContext.LoginId;
			return View("Index", model);
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
			criteria.Filters.Where(x => x.Name.StartsWith("$")).ToList().ForEach(x => criteria.Filters.Remove(x));
			var spCriteria = Mapper.Map<SearchCriteria>(criteria);
			var results = _searchProvider.Search<SearchResult>(spCriteria, _sessionProvider, _userContext);
			var refinerMenu = Mapper.Map<List<JsonTaxonomyMenuItem>>(results.RefinerResults);
			refinerMenu.AddRange(_additionalRefinersFactory.GetRefiners(spCriteria));
			CurrentUserRefinementEnhancer(ref refinerMenu);
			var sortList = GetSort(criteria);
			if (sortList.Count > 0)
				refinerMenu.Sort(new SortListRefinerComparer(sortList));

			foreach (var jsonTaxonomyMenuItem in refinerMenu)
			{
				jsonTaxonomyMenuItem.Key = jsonTaxonomyMenuItem.Key.Replace(".", "___");
				setAllCount(jsonTaxonomyMenuItem.Children, results.SearchCriteria.Paging.TotalResults);
			}
			return Json(refinerMenu);
		}

		private void setAllCount(IEnumerable<JsonTaxonomyMenuItem> items, long totalCount)
		{
			var allItem = items.FirstOrDefault(x => x.Text.ToLower() == "all");
			if (allItem != null)
				allItem.Count = totalCount;
		}

		private void CurrentUserRefinementEnhancer(ref List<JsonTaxonomyMenuItem> results)
		{
			var refinerGroup = results.FirstOrDefault(x => x.Key == AssetFieldNames.AriaProjectHandler);

			if (refinerGroup != null && !refinerGroup.Children.Any(x => x.IsCurrentUserSpecific))
			{
				refinerGroup.Children.Insert(1, new JsonTaxonomyMenuItem
				{
					Key = refinerGroup.Key,
					Text = "'Assigned to me",
					RefinementValue = User.Identity.Name
				});
			}
		}

		/// <summary>
		/// Refiners the specified criteria.
		/// </summary>
		/// <param name="criteria">The criteria.</param>
		/// <returns></returns>
		[AuthorizeClaim(Resource = SecuredResources.ProjectTemplateAdministration, Action = SecuredActions.View)]
		public ActionResult ProjectTemplateRefiners(JsonSearchCriteria criteria)
		{
			criteria.Paging.PageSize = 1;
			criteria.Paging.Page = 1;
			criteria.Filters.Where(x => x.Name.StartsWith("$")).ToList().ForEach(x => criteria.Filters.Remove(x));

			var spCriteria = Mapper.Map<SearchCriteria>(criteria);
			var results = _projectTemplateProvider.Search(spCriteria, _userContext);
			var refinerMenu = Mapper.Map<List<JsonTaxonomyMenuItem>>(results.RefinerResults);
			if (refinerMenu.Any())
			{
				foreach (var item in refinerMenu)
				{
					if (item.Children.Any() && item.Children[0].Text.ToLower() == "all")
						item.Children[0].Count = results.SearchCriteria.Paging.TotalResults;
				}
			}
			return Json(refinerMenu);
		}

		private List<string> GetSort(JsonSearchCriteria criteria)
		{
			if (criteria.EntityType == EntityType.Task)
				return new List<string>
				{
					AssetFieldNames.AriaCompanyId,
					AssetFieldNames.AriaTaskOwner,
					AssetFieldNames.AriaTaskPhase,
					AssetFieldNames.AriaTaskProgressCal
				};

			if (criteria.EntityType == EntityType.Project)
				return new List<string>
				{
					AssetFieldNames.AriaOrderOwner,
					AssetFieldNames.AriaProjectHasOrderNumber,
					AssetFieldNames.AriaProjectIndustryCode,
					AssetFieldNames.AriaProjectServiceCode,
					AssetFieldNames.AriaProjectLocationName,
					AssetFieldNames.AriaCompanyId,
					AssetFieldNames.AriaProjectProjectStatus,
					AssetFieldNames.AriaProjectHandler,
					AssetFieldNames.AriaProjectExpedited,
					AssetFieldNames.AriaProjectEndDate,
					AssetFieldNames.AriaProjectTaskMinimumDueDate
				};

			if (criteria.EntityType == EntityType.Order)
				return new List<string>
				{
					AssetFieldNames.AriaOrderStatus,
					AssetFieldNames.AriaOrderIndustryCode,
					AssetFieldNames.AriaOrderServiceCode,
					AssetFieldNames.AriaOrderLocationCode,
					AssetFieldNames.AriaCompanyId
				};


			return new List<string>();
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

				switch (entityType)
				{
					case EntityType.Product:
						criteria.ApplyProductSearch(_userContext);
						break;
					case EntityType.Order:
						criteria.ApplyOrderSearch(_userContext);
						break;
					case EntityType.Project:
						criteria.ApplyProjectSearch(_userContext);
						break;
					case EntityType.ProjectTemplate:
						criteria.ApplyProjectTemplateSearch();
						break;
				}

				return criteria;
			});
			criteria = SearchCriteriaExtensions.SetSearchCriteriaFromTempIfNeeded(entityType, setSearchCriteriaAction, TempData);
			return criteria ?? new SearchCriteria();
		}

		/// <summary>
		///     Gets/Pages the Products for the specified search criteria.
		/// </summary>
		/// <param name="criteria">The criteria.</param>
		/// <param name="useDefaultSearch">Default search.</param>
		/// <returns></returns>
		public ActionResult Product(SearchCriteria criteria, bool useDefaultSearch = false)
		{
			criteria = UseDefaultSearchCriteria(criteria, EntityType.Product, useDefaultSearch);

			var model = SearchWithFavorite<ProductSearchResult>(criteria);
			ViewBag.PageLinks = HomeController.BuildPageLinks(_userContext, Url, EntityType.Product.ToString());
			var jsonSearchCriteria = Mapper.Map<JsonSearchCriteria>(model.SearchCriteria);
			ViewBag.JsonCriteria = jsonSearchCriteria;

			var defaultSearchCriteria = new SearchCriteria
			{
				EntityType = EntityType.Product,
				Paging = new Paging { Page = 1, PageSize = 1 }
			};
			defaultSearchCriteria = defaultSearchCriteria.ApplyProductSearch(_userContext);
			var defaultSearchResult = _searchProvider.Search<ProductSearchResult>(defaultSearchCriteria, _sessionProvider,
				_userContext);
			ViewBag.JsonDefaultCriteria = Mapper.Map<JsonSearchCriteria>(defaultSearchResult.SearchCriteria);

			model.ShouldShowFilters = true;
			return View("Index", model);
		}

		/// <summary>
		///     Gets/Pages the Product Families for the specified search criteria.
		/// </summary>
		/// <param name="criteria">The criteria.</param>
		/// <returns></returns>
		public ActionResult ProductTemplate(SearchCriteria criteria)
		{
			criteria.ApplyProductFamilySearch();
			var model = _productFamilyProvider.Search(criteria, _userContext);

			model.PageLinks = HomeController.BuildPageLinks(_userContext, Url, EntityType.Product.ToString());
			_searchProvider.BuildRefiners(model.PageLinks, model.RefinerResults, criteria, _userContext);

			SetViewBag(criteria);
			model.PageActions = ActionsRight(EntityType.ProductFamily);
			ViewBag.PageActions = ActionsRight(EntityType.ProductFamily);
			return View("Index", model);
		}

		/// <summary>
		/// Projects the template.
		/// </summary>
		/// <param name="criteria">The criteria.</param>
		/// <param name="useDefaultSearch">if set to <c>true</c> [use default search].</param>
		/// <returns></returns>
		[AuthorizeClaim(Resource = SecuredResources.ProjectTemplateAdministration, Action = SecuredActions.View)]
		public ActionResult ProjectTemplate(SearchCriteria criteria, bool useDefaultSearch = false)
		{
			criteria = UseDefaultSearchCriteria(criteria, EntityType.ProjectTemplate, useDefaultSearch);

			var controller = ControllerContext.RouteData.Values["controller"] as string;
			var action = ControllerContext.RouteData.Values["action"] as string;
			var location = @"/" + controller + @"/" + action;


			var availableDefaults = _profileProvider.FetchAvailableDefaultsByLocation(location, _userContext);
			var searchFavorites = availableDefaults as IList<SearchFavorite> ?? availableDefaults.ToList();
			SearchFavorite favoriteSearch = searchFavorites.FirstOrDefault(x => x.ActiveDefault);
			if (useDefaultSearch && favoriteSearch != null)
				criteria = favoriteSearch.Criteria;

			var model = _projectTemplateProvider.Search(criteria, _userContext);
			model.ActiveDefault = favoriteSearch;
			model.AvailableDefaults = searchFavorites;

			model.AppliedFilters.ForEach(x => _searchProvider.FormatRefiner(x, _userContext));
			model.PageLinks = HomeController.BuildPageLinks(_userContext, Url,
				EntityType.ProjectTemplate.ToString());
			ViewBag.PageLinks = model.PageLinks;
			model.SearchCriteria.PrimaryRefiner = _searchProvider.BuildRefiners(model.PageLinks,
				model.RefinerResults, criteria, _userContext);

			SetViewBag(criteria);
			model.Breadcrumbs = ViewBag.BreadCrumbs;
			model.PageActions = ActionsRight(EntityType.ProjectTemplate);
			ViewBag.PageActions = model.PageActions;

			var jsonSearchCriteria = Mapper.Map<JsonSearchCriteria>(model.SearchCriteria);
			ViewBag.JsonCriteria = jsonSearchCriteria;

			var defaultSearchCriteria = new SearchCriteria
			{
				EntityType = EntityType.ProjectTemplate,
				Paging = new Paging { Page = 1, PageSize = 1 }
			};
			defaultSearchCriteria = defaultSearchCriteria.ApplyProjectTemplateSearch();
			var defaultSearchResult = _projectTemplateProvider.Search(defaultSearchCriteria, _userContext);
			ViewBag.JsonDefaultCriteria = Mapper.Map<JsonSearchCriteria>(defaultSearchResult.SearchCriteria);

			model.ShouldShowFilters = true;
			return View("Index", model);
		}

		/// <summary>
		/// Forward.
		/// </summary>
		/// <param name="searchCriteria">The criteria as string Json.</param>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="saveFilterName">The name to save the current criteria into saved favorites etc.</param>
		/// <returns></returns>
		public ActionResult Forward(string searchCriteria, ProjectViewType viewType = ProjectViewType.Default,
			string saveFilterName = null)
		{
			var decoded = HttpUtility.HtmlDecode(searchCriteria);
			var jsonSearchCriteria = decoded.FromJson<JsonSearchCriteria>();
			var objectSearchCriteria = Mapper.Map<SearchCriteria>(jsonSearchCriteria);
			var urlNoQs = Url.GetReferAbsolutePath();
			if (!saveFilterName.IsNullOrWhiteSpace())
			{
				var searchFavorite = new SearchFavorite
				{
					Criteria = objectSearchCriteria,
					PageUrl = urlNoQs,
					Title = saveFilterName,
					ActiveDefault = true,
					AvailableDefault = true
				};
				_profileProvider.CreateFavoriteSearch(_userContext.UserId, searchFavorite, Session, _sessionProvider, _userContext);
			}

			string redirectUrl = urlNoQs + "?viewType=" + viewType;

			if (objectSearchCriteria != null)
			{
				var querySearchCriteria = objectSearchCriteria.ToQueryString();
				var tempqueryString = redirectUrl + "&" + querySearchCriteria;
				// IE 2047 Max URL length, other major browsers allow for longer URL lengths[2048 - We can't tell browser truncated or not]
				if (!string.IsNullOrWhiteSpace(tempqueryString) && tempqueryString.Length >= 2047)
				{
					if (TempData.ContainsKey("Criteria"))
					{
						TempData.Remove("Criteria");
					}
					TempData.Add("Criteria", objectSearchCriteria);
				}
				else
				{
					//If search criteria is null we are redirecting to home page. The typical scenario is when the query string is more than 1848 and session timeouts 
					redirectUrl += "&" + querySearchCriteria;
				}
			}
			return Redirect(redirectUrl);
		}



		/// <summary>
		/// Projects the specified criteria.
		/// </summary>
		/// <param name="criteria">The criteria.</param>
		/// <param name="useDefaultSearch">Default search.</param>
		/// <param name="viewType">Type of the view.</param>
		/// <returns></returns>
		public ActionResult Project(SearchCriteria criteria, bool useDefaultSearch = false,
			ProjectViewType viewType = ProjectViewType.Default)
		{
			criteria = UseDefaultSearchCriteria(criteria, EntityType.Project, useDefaultSearch);
			ViewBag.PageLinks = HomeController.BuildPageLinks(_userContext, Url, EntityType.Project.ToString());

			SearchResultSet<ProjectSearchResult> model;
			if (viewType != ProjectViewType.Default &&
				IsRequestMergeAllowed(criteria))
			{
				var bigPageCriteria = criteria.ToJson().FromJson<SearchCriteria>();
				bigPageCriteria.Paging.PageSize = (bigPageCriteria.Paging.PageSize < 50)
					? bigPageCriteria.Paging.PageSize * 10
					: 500;
				bigPageCriteria.Paging.Page = 1;
				model = SearchWithFavorite<ProjectSearchResult>(bigPageCriteria);
				FillProject(model, criteria);
			}
			else
			{
				model = SearchWithFavorite<ProjectSearchResult>(criteria);
				if (viewType != ProjectViewType.Default)
					FillTasksCountsForProjects(model);
			}

			EnrichResultsWithCompanyName(model);

			if (_userContext.CanActOnProject())
			{
				ViewBag.PopUpActions = new List<TaxonomyMenuItem>
				{
					//note that if this Text changes, we probably want to keep the menu option in ProjectSearchResult.cshtml in sync
					new TaxonomyMenuItem {Text = "Export to Excel", Url = Url.ProjectDownloadProjects()}
				};
			}

			ViewBag.TableViewLink = Url.Action("Project", new { viewType = ProjectViewType.Grid }) + "&" + criteria.ToQueryString();
			ViewBag.isUlEmployee = _userContext.IsUlEmployee;
			ViewBag.ViewType = viewType;
			var jsonSearchCriteria = Mapper.Map<JsonSearchCriteria>(model.SearchCriteria);
			jsonSearchCriteria.AddCompanyAutoCompleteRefinerDisplay(Url)
				.AddProjectHandlerAutoCompleteRefinerDisplay(Url)
				.AddProjectEcdDatePickerRefinerDisplay();
			if (viewType == ProjectViewType.Grid)
				jsonSearchCriteria.AddOrderOwnerAutoCompleteRefinerDisplay(Url);

			ViewBag.JsonCriteria = jsonSearchCriteria;

			var defaultSearchCriteria = new SearchCriteria
			{
				EntityType = EntityType.Project,
				Paging = new Paging { Page = 1, PageSize = 1 }
			};
			defaultSearchCriteria = defaultSearchCriteria.ApplyProjectSearch(_userContext, viewType);
			var defaultSearchResult = _searchProvider.Search<ProjectSearchResult>(defaultSearchCriteria, _sessionProvider,
				_userContext);
			var jsonCriterTemp = Mapper.Map<JsonSearchCriteria>(defaultSearchResult.SearchCriteria);
			jsonCriterTemp.AddCompanyAutoCompleteRefinerDisplay(Url)
				.AddProjectHandlerAutoCompleteRefinerDisplay(Url)
				.AddProjectEcdDatePickerRefinerDisplay();

			if (viewType == ProjectViewType.Grid)
				jsonCriterTemp.AddOrderOwnerAutoCompleteRefinerDisplay(Url);

			ViewBag.JsonDefaultCriteria = jsonCriterTemp;

			ViewBag.AuxiliaryFilters = _additionalRefinersFactory.GetRefiners(criteria);
			return GetProjectView(viewType, model, criteria);
		}

		/// <summary>
		/// Enriches the name of the results with company.
		/// This process uses a dictionary so that repeated ids are not fetched more than onces from the data source.
		/// </summary>
		/// <param name="model">The model.</param>
		internal void EnrichResultsWithCompanyName(SearchResultSet<ProjectSearchResult> model)
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

		/// <summary>
		/// Gets the project list.
		/// </summary>
		/// <param name="criteria">The criteria.</param>
		/// <param name="postData">The post data.</param>
		/// <returns></returns>
		public ActionResult GetProjectList(JsonSearchCriteria criteria, JqGridPostData postData)
		{
			var spcriteria = Mapper.Map<SearchCriteria>(criteria);

			if (spcriteria == null) // || criteria.EntityType == null)
			{
				spcriteria = new SearchCriteria();
				spcriteria.ApplyProjectSearch(_userContext);
			}
			if (spcriteria.Refiners == null || spcriteria.Refiners.Count == 0)
			{
				spcriteria.ApplyProjectSearch(_userContext);
			}
			if (spcriteria.EntityType == null)
			{
				spcriteria.EntityType = EntityType.Project;
			}
			CleanFilterSubscripts(spcriteria);
			MapJqGridDataToSearchCriteria(spcriteria, postData);
			HashSet<Guid> projectHashLookup;

			SearchResultSet<ProjectSearchResult> model;
			if (IsRequestMergeAllowed(spcriteria))
			{
				var bigPageCriteria = spcriteria.ToJson().FromJson<SearchCriteria>();
				bigPageCriteria.Paging.PageSize = (bigPageCriteria.Paging.PageSize < 50)
					? bigPageCriteria.Paging.PageSize * 10
					: 500;
				bigPageCriteria.Paging.Page = 1;
				model = SearchWithFavorite<ProjectSearchResult>(bigPageCriteria);
				projectHashLookup = new HashSet<Guid>(model.Results.Select(x => x.Id));
				AssureFilledRequests(model, spcriteria);
			}
			else
			{
				model = SearchWithFavorite<ProjectSearchResult>(spcriteria);
				projectHashLookup = new HashSet<Guid>(model.Results.Select(x => x.Id));
			}

			FillTasksCountsForProjects(model);
			EnrichResultsWithCompanyName(model);

			var facadeList = (from obj in model.Results
							  let rowType = projectHashLookup.Contains(obj.Id) ? RowType.Project : RowType.Request
							  select new ProjectRequestRowFacade
							  {
								  Id = obj.ProjectId,
								  AssignedTaskCount = obj.AssignedTaskCount,
								  CompanyId = obj.ContainerId,
								  CompanyName = obj.CompanyName,
								  CompletedTaskCount = obj.CompletedTaskCount,
								  CompletionDate = obj.CompletionDate.ToDateString(),
								  DateBooked = obj.DateBooked.ToDateString(),
								  DateBookedRaw = obj.DateBooked,
								  EndDate = obj.EndDate.ToDateString(),
								  EndDateRaw = obj.EndDate,
								  OrderId = obj.OrderId,
								  OrderNumber = obj.OrderNumber,
								  OrderStatus = obj.OrderStatus,
								  PastDueTaskCount = obj.PastDueTaskCount,
								  ProjectDueDate = obj.ProjectDueDate.ToDateString(),
								  ProjectDueDateRaw = obj.ProjectDueDate,
								  ProjectHandler = obj.ProjectHandler,
								  ProjectName = obj.ProjectName,
								  ProjectStatus = obj.ProjectStatus,
								  RowType = rowType,
								  ServiceLineItemCount = obj.ServiceLineItemCount,
								  TotalTasks = obj.TotalTasks,
								  OrderOwner = obj.OrderOwner
							  }).ToList();

			return Json((new JqGridResults<ProjectRequestRowFacade>
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

		internal void AssureFilledRequests(SearchResultSet<ProjectSearchResult> model, SearchCriteria criteria)
		{
			if (IsRequestMergeAllowed(criteria))
				FillRequests(model, criteria, criteria.SortField, criteria.SortOrder);
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
			criteria.SortOrder = (postData.sord == "asc")
				? SortDirectionDto.Ascending
				: SortDirectionDto.Descending;
			criteria.SortField = postData.sidx;
		}

		/// <summary>
		/// Gets the project view based on the specified viewType.
		/// </summary>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="model">The model.</param>
		/// <param name="criteria"></param>
		/// <returns></returns>
		internal ViewResult GetProjectView(ProjectViewType viewType, SearchResultSet<ProjectSearchResult> model,
			SearchCriteria criteria)
		{
			if (!_userContext.IsUlEmployee)
			{
				return View("Index", model);
			}

			switch (viewType)
			{
				case ProjectViewType.Grid:
					return View("~/Views/search/ProjectMultiViewWide.cshtml", model);
				default:
					return View("Index", model);
			}
		}

		internal void FillProject(SearchResultSet<ProjectSearchResult> model, SearchCriteria criteria)
		{
			FillRequests(model, criteria, criteria.SortField, criteria.SortOrder);
			FillTasksCountsForProjects(model);
		}

		internal void FillTasksCountsForProjects(SearchResultSet<ProjectSearchResult> model)
		{
			var activeFilter = string.Format("NOT({0}=000400) AND NOT({0}=000500)", AssetFieldNames.AriaTaskPhase);
			var searchCriteria = new SearchCriteria
			{
				Paging = new Paging { EndIndex = 1, Page = 1, PageSize = 1 },
				Query =
					string.Format("{0}<{1} AND {2}", AssetFieldNames.AriaTaskDueDate, DateTime.UtcNow.ToString("yyyy-MM-dd"),
						activeFilter)
			};
			var projectSearchResults = model.Results; //.Skip(i).Take(10);
			foreach (var projectSearchResult in projectSearchResults)
			{
				searchCriteria.ApplyContainerFilter(projectSearchResult.ContainerId);
			}
			searchCriteria.ApplyTaskSearch();
			searchCriteria.Refiners.Add(AssetFieldNames.AriaContainerId);
			var overDueResultSet = _searchProvider.Search<TaskSearchResult>(searchCriteria, _sessionProvider, _userContext);

			searchCriteria.Query = "";
			var totalTasksResultSet = _searchProvider.Search<TaskSearchResult>(searchCriteria, _sessionProvider, _userContext);

			searchCriteria.Query = "ariaTaskPhase=000400";
			var completedTasksResultSet = _searchProvider.Search<TaskSearchResult>(searchCriteria, _sessionProvider, _userContext);

			searchCriteria.Query = "ariaTaskPhase=000500";
			var cancelledTasksResultSet = _searchProvider.Search<TaskSearchResult>(searchCriteria, _sessionProvider, _userContext);
			// do this last b/c
			searchCriteria.Query = activeFilter;
			searchCriteria.Filters.Add(AssetFieldNames.AriaTaskOwner, new List<string> { "Unassigned" });
			var unassignedTasksResultSet = _searchProvider.Search<TaskSearchResult>(searchCriteria, _sessionProvider,
				_userContext);


			foreach (var projectSearchResult in projectSearchResults)
			{
				projectSearchResult.TotalTasks = GetTaskCount(totalTasksResultSet, projectSearchResult);
				projectSearchResult.CompletedTaskCount = GetTaskCount(completedTasksResultSet, projectSearchResult);
				projectSearchResult.PastDueTaskCount = GetTaskCount(overDueResultSet, projectSearchResult);
				var cancelledTaskCount = GetTaskCount(cancelledTasksResultSet, projectSearchResult);
				projectSearchResult.AssignedTaskCount = Math.Max(0,
					projectSearchResult.TotalTasks - GetTaskCount(unassignedTasksResultSet, projectSearchResult) -
					projectSearchResult.CompletedTaskCount - cancelledTaskCount);
			}
		}

		private static int GetTaskCount(SearchResultSet<TaskSearchResult> totalTasksResultSet,
			ProjectSearchResult projectSearchResult)
		{
			var count = 0;
			if (null == totalTasksResultSet || null == totalTasksResultSet.RefinerResults ||
				!totalTasksResultSet.RefinerResults.ContainsKey(AssetFieldNames.AriaContainerId))
				return 0; //this will happen if NO results returned by a query.
			var refinementItems =
				totalTasksResultSet.RefinerResults[AssetFieldNames.AriaContainerId].FirstOrDefault(
					x => x.Value == projectSearchResult.ContainerId.ToString());
			if (null != refinementItems)
				count = (int)refinementItems.Count;
			return count;
		}

		internal void FillRequests(SearchResultSet<ProjectSearchResult> model, SearchCriteria criteria, string sortField,
			SortDirectionDto? sortDirectionDto = SortDirectionDto.Ascending)
		{
			var combined = model.Results.ToList();
			var requestSearchCriteria = criteria.ToJson().FromJson<SearchCriteria>();
			requestSearchCriteria.Paging = new Paging { Page = 1, PageSize = 10000 };
			requestSearchCriteria.EntityType = EntityType.InboundOrder;
			var requests = _incomingOrderProvider.Search(requestSearchCriteria, _userContext, new HashSet<Guid>());

			foreach (var request in requests.Results)
			{
				var mappedResult = new ProjectSearchResult
				{
					Id = request.Id,
					ChangeDate = request.IncomingOrder.UpdatedDateTime,
					CompanyId = request.IncomingOrder.CompanyId,
					ProjectId = request.IncomingOrder.Id,
					ServiceLineItemCount = request.IncomingOrder.ServiceLines.Count,
					OrderStatus = request.IncomingOrder.Status,
					DateBooked = request.IncomingOrder.DateBooked,
					OrderNumber = request.IncomingOrder.OrderNumber,
					EntityType = EntityType.InboundOrder
				};
				if (mappedResult.CompanyId.HasValue && mappedResult.CompanyId != default(Guid))
				{
					mappedResult.CompanyName =
						_companyProvider.FetchDisplayNameById(mappedResult.CompanyId.Value);
				}
				combined.Add(mappedResult);
			}

			IOrderedEnumerable<ProjectSearchResult> final = null;
			if (sortField == AssetFieldNames.AriaOrderNumber)
			{
				final = sortDirectionDto == SortDirectionDto.Descending
					? combined.OrderByDescending(x => x.OrderNumber).ThenByDescending(x => x.ProjectName)
					: combined.OrderBy(x => x.OrderNumber).ThenBy(x => x.ProjectName);
			}
			else if (sortField == AssetFieldNames.AriaCompanyName)
			{
				final = sortDirectionDto == SortDirectionDto.Descending
					? combined.OrderByDescending(x => x.CompanyName).ThenByDescending(x => x.OrderNumber)
					: combined.OrderBy(x => x.CompanyName).ThenBy(x => x.OrderNumber);
			}
			else if (sortField == AssetFieldNames.AriaDateBooked)
			{
				final = sortDirectionDto == SortDirectionDto.Descending
					? combined.OrderByDescending(x => x.DateBooked).ThenByDescending(x => x.OrderNumber)
					: combined.OrderBy(x => x.DateBooked).ThenBy(x => x.OrderNumber);
			}

			FinalizeRequestMerge(model, criteria, final);

			//sets the project name on Requests that are unassigned to such.
			combined.Where(x => x.EntityType == EntityType.InboundOrder && string.IsNullOrWhiteSpace(x.ProjectName))
				.ForEach(x => x.ProjectName = "Unassigned");
		}

		private static void FinalizeRequestMerge(SearchResultSet<ProjectSearchResult> model, SearchCriteria criteria,
			IOrderedEnumerable<ProjectSearchResult> final)
		{
			var total = final.Count();
			long start = criteria.Paging.StartIndex;
			var endIndex = Math.Min(start + criteria.Paging.PageSize, total) - 1;
			criteria.Paging = new Paging(start, endIndex, total, criteria.Paging.PageSize);

			var finalSorted = final.Skip((int)start).Take(criteria.Paging.PageSize).ToList();

			model.Results.Clear();
			model.Results.AddRange(finalSorted);
			model.SearchCriteria.Paging = criteria.Paging;
			model.SearchCriteria.Paging.EndIndex = endIndex;
			model.SearchCriteria.Paging.TotalResults = total;
		}

		internal static bool IsRequestMergeAllowed(SearchCriteria criteria)
		{
			if (criteria.SortField == AssetFieldNames.AriaOrderNumber || criteria.Sorts.Any(
				y =>
					y.FieldName == AssetFieldNames.AriaOrderNumber))
			{
				var sortDirectionDto = !string.IsNullOrEmpty(criteria.SortField)
					? criteria.SortOrder
					: criteria.Sorts.First(x => x.FieldName == AssetFieldNames.AriaOrderNumber).Order;
				criteria.SortField = AssetFieldNames.AriaOrderNumber;
				criteria.SortOrder = sortDirectionDto;
			}
			var allowedFilters = new[]
			{
				AssetFieldNames.AriaCompanyName, AssetFieldNames.AriaProjectIndustryCode,
				AssetFieldNames.AriaProjectLocationName, AssetFieldNames.AriaProjectIndustryCode
			};
			var allowedSorts = new[]
			{
				AssetFieldNames.AriaCompanyName, AssetFieldNames.AriaOrderNumber, AssetFieldNames.AriaDateBooked
			};
			if (criteria.Filters.Any() && !criteria.Filters.Keys.Join(allowedFilters, x => x, y => y, (x, y) => x == y).Any())
			{
				return false;
			}

			if (allowedSorts.All(x => x != criteria.SortField))
			{
				return false;
			}
			return true;
		}

		private SearchResultSet<TResult> SearchWithFavorite<TResult>(SearchCriteria criteria, bool lazyLoad = false)
			where TResult : SearchResult
		{
			var controller = ControllerContext.RouteData.Values["controller"] as string;
			var action = ControllerContext.RouteData.Values["action"] as string;
			var location = @"/" + controller + @"/" + action;

			var availableDefaults = _profileProvider.FetchAvailableDefaultsByLocation(location, _userContext);
			IEnumerable<SearchFavorite> searchFavorites = availableDefaults as SearchFavorite[] ?? availableDefaults.ToArray();
			SearchFavorite favoriteSearch = searchFavorites.FirstOrDefault(x => x.ActiveDefault);
			SearchResultSet<TResult> model;
			if (!lazyLoad)
			{
				model = Search<TResult>(criteria);
			}
			else
				model = new SearchResultSet<TResult>(_userContext)
				{
					SearchCriteria = criteria
				};

			model.ActiveDefault = favoriteSearch;
			model.AvailableDefaults = searchFavorites;
			model.ShouldShowFilters = true;
			return model;
		}

		/// <summary>
		///     Orders the specified criteria.
		/// </summary>
		/// <param name="criteria">The criteria.</param>
		/// <param name="useDefaultSearch">Default search.</param>
		/// <returns></returns>
		[AuthorizeClaim(Resource = SecuredResources.OrderInstance, Action = SecuredActions.View)]
		public ActionResult Order(SearchCriteria criteria, bool useDefaultSearch = false)
		{
			criteria = UseDefaultSearchCriteria(criteria, EntityType.Order, useDefaultSearch);

			var model = SearchWithFavorite<OrderSearchResult>(criteria);
			ViewBag.PageLinks = HomeController.BuildPageLinks(_userContext, Url, EntityType.Order.ToString());
			var jsonSearchCriteria = Mapper.Map<JsonSearchCriteria>(model.SearchCriteria);
			jsonSearchCriteria.AddCompanyAutoCompleteRefinerDisplay(Url);
			ViewBag.JsonCriteria = jsonSearchCriteria.EscapeFilterKeys().EscapeRefinerKeys();

			var defaultSearchCriteria = new SearchCriteria
			{
				EntityType = EntityType.Order,
				Paging = new Paging { Page = 1, PageSize = 1 }
			};
			defaultSearchCriteria = defaultSearchCriteria.ApplyOrderSearch(_userContext);
			var defaultSearchResult = _searchProvider.Search<OrderSearchResult>(defaultSearchCriteria, _sessionProvider,
				_userContext);
			var jsonCriteriaTemp =
				Mapper.Map<JsonSearchCriteria>(defaultSearchResult.SearchCriteria)
					.EscapeFilterKeys()
					.EscapeRefinerKeys();

			jsonCriteriaTemp.AddCompanyAutoCompleteRefinerDisplay(Url);
			ViewBag.JsonDefaultCriteria = jsonCriteriaTemp;

			model.ShouldShowFilters = true;
			return View("Index", model);
		}

		/// <summary>
		/// Tasks the specified criteria.
		/// </summary>
		/// <param name="searchCriteria">The search criteria.</param>
		/// <param name="useDefaultSearch">if set to <c>true</c> [use default search].</param>
		/// <returns></returns>
		[ULEmployee]
		public ActionResult Task(SearchCriteria searchCriteria, bool useDefaultSearch = false)
		{
			ActionResult defaultRedirect;
			if (ShouldRedirect(useDefaultSearch, _profileProvider, Url.PageSearchTasks(), out defaultRedirect))
				return defaultRedirect;

			var setSearchCriteriaAction = new Func<SearchCriteria>(() =>
			{
				// ReSharper disable once AccessToModifiedClosure
				searchCriteria.ApplyTaskSearch();
				// ReSharper disable once AccessToModifiedClosure
				return searchCriteria;
			});
			searchCriteria = SearchCriteriaExtensions.SetSearchCriteriaFromTempIfNeeded(EntityType.Task, setSearchCriteriaAction,
				TempData);

			if (searchCriteria == null)
				return null;

			var model = SearchWithFavorite<TaskSearchResult>(searchCriteria);

			var pageActions = new List<TaxonomyMenuItem>();
			var queryString = searchCriteria.ToQueryString();
			pageActions.Add(new TaxonomyMenuItem
			{
				Key = "exportToExcel",
				Text = "Export to Excel",
				Url = string.Format("{0}?{1}", Url.TaskExport(), queryString),
				Modal = false,
				LinkData = { { "class", "primary arrow" } }
			});
			pageActions.AddRange(model.PageActions);

			ViewBag.TopActions = pageActions;

			ViewBag.PageLinks = HomeController.BuildPageLinks(_userContext, Url, EntityType.Task.ToString());
			var jsonSearchCriteria = Mapper.Map<JsonSearchCriteria>(model.SearchCriteria);
			jsonSearchCriteria.AddCompanyAutoCompleteRefinerDisplay(Url)
				.AddTaskOwnerAutoCompleteRefinerDisplay(Url);

			ViewBag.JsonCriteria = jsonSearchCriteria;

			var defaultSearchCriteria = new SearchCriteria
			{
				EntityType = EntityType.Task,
				Paging = new Paging { Page = 1, PageSize = 1 }
			};
			defaultSearchCriteria = defaultSearchCriteria.ApplyTaskSearch();
			var defaultSearchResult = _searchProvider.Search<TaskSearchResult>(defaultSearchCriteria, _sessionProvider,
				_userContext);
			var jsonCriterTemp = Mapper.Map<JsonSearchCriteria>(defaultSearchResult.SearchCriteria);
			jsonCriterTemp.AddCompanyAutoCompleteRefinerDisplay(Url)
				.AddTaskOwnerAutoCompleteRefinerDisplay(Url);

			SetProjectHandler(model);
			ViewBag.JsonDefaultCriteria = jsonCriterTemp;
			ViewBag.ShowActionMenu = true;

			return View("~/Views/Task/Index.cshtml", model);
		}

		private void SetProjectHandler(SearchResultSet<TaskSearchResult> tasks)
		{
			var searchCriteria = new SearchCriteria { EntityType = EntityType.Project };
			searchCriteria.Filters.Add(AssetFieldNames.AriaProjectId, tasks.Results.Select(x => x.ProjectId.ToString()).ToList());
			searchCriteria.Paging = new Paging { PageSize = tasks.Results.Count, Page = 1 };
			var projects = _searchProvider.Search<ProjectSearchResult>(searchCriteria, _sessionProvider, _userContext);
			foreach (var project in projects.Results)
			{
				var project1 = project;
				tasks.Results.Where(x => x.ProjectId == project1.ProjectId).ForEach(x =>
				{
					x.ProjectHandler = project1.ProjectHandler;
					x.ProjectStatus = project1.ProjectStatus;
				});
			}
		}

		/// <summary>
		///     Export tasks based on specified criteria
		/// </summary>
		/// <param name="criteria">The criteria.</param>
		/// <returns></returns>
		[ULEmployee]
		public FileStreamResult ExportTasks(SearchCriteria criteria)
		{
			criteria.Paging.Page = 1;
			criteria.Paging.PageSize = SearchCriteria.MaxPageSize;
			criteria.Paging.EndIndex = criteria.Paging.PageSize - 1;
			var fileData = _taskProvider.DownloadSearch(criteria);

			return File(fileData.OutputStream, fileData.ContentType, fileData.FileName);
		}

		/// <summary>
		///     Remembers the specified result, in the group specified, for this session.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="group">The group.</param>
		/// <returns></returns>
		[HttpPost]
		public JsonResult GroupItem(Guid id, string group)
		{
			var total = _sessionProvider.SaveGroupItem(id, group);
			var model = new SelectionModel
			{
				GroupName = group,
				TotalCount = total
			};

			return Json(model);
		}

		/// <summary>
		///     Removes the specified result from the group specified.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="group">The group.</param>
		/// <param name="notUsed">This variable is not used, only here to avoid a method naming conflict.</param>
		/// <returns></returns>
		[HttpDelete]
		public JsonResult GroupItem(Guid id, string group, string notUsed)
		{
			var total = _sessionProvider.RemoveGroupItem(id, group);
			var model = new SelectionModel
			{
				GroupName = group,
				TotalCount = total
			};

			return Json(model);
		}

		/// <summary>
		///     returns the selected items for the group specified.
		/// </summary>
		/// <param name="id">The group name.</param>
		/// <returns></returns>
		[HttpGet]
		public JsonResult GroupItem(string id)
		{
			var items = _sessionProvider.GetGroupItems(id);
			var model = new SelectionModel
			{
				GroupName = id,
				TotalCount = items.Count,
				Items = items
			};

			return Json(model, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// Returns JSON for typeahead queries.
		/// </summary>
		/// <param name="criteria">The criteria.</param>
		/// <returns></returns>
		public JsonResult Typeahead(SearchCriteria criteria)
		{
			criteria = criteria.ApplyGlobalSearchFilter();
			var model = Search<SearchResult>(criteria);

			return Json(model.Results, JsonRequestBehavior.AllowGet);
		}

		private SearchResultSet<TResult> Search<TResult>(SearchCriteria searchCriteria)
			where TResult : SearchResult
		{
			var results = _searchProvider.Search<TResult>(searchCriteria, _sessionProvider, _userContext);
			// disable actions on any global searches, they should only be available from within project/order context.
			results.Results.ForEach(x =>
			{
				var y = x as DocumentSearchResult;
				if (null == y)
					return;
				y.CanDelete = y.CanEdit = y.CanReUpload = false;
			}
				);
			results.AppliedFilters.ForEach(x => _searchProvider.FormatRefiner(x, _userContext));
			results.PageLinks = HomeController.BuildPageLinks(_userContext, Url,
				results.SearchCriteria.EntityType.ToString());

			results.SearchCriteria.PrimaryRefiner = _searchProvider.BuildRefiners(results.PageLinks,
				results.RefinerResults, searchCriteria, _userContext);

			SetViewBag(searchCriteria);
			results.Breadcrumbs = ViewBag.BreadCrumbs;
			if (searchCriteria.EntityType.HasValue)
			{
				results.PageActions = ActionsRight(searchCriteria.EntityType.Value);
				ViewBag.PageActions = results.PageActions;
			}

			return results;
		}

		/// <summary>
		///     Sets common page elements, titles, etc
		/// </summary>
		/// <param name="searchCriteria"></param>
		private void SetViewBag(SearchCriteria searchCriteria)
		{
			var pageTitle = searchCriteria.EntityType.HasValue
				? searchCriteria.EntityType.Value.GetDisplayName().Pluralize()
				: "Search";
			var searchCrumb = new Breadcrumb { Text = pageTitle, Url = Url.Action(null) };

			SetPageMetadata(pageTitle, new[] { searchCrumb });
		}

		internal List<TaxonomyMenuItem> ActionsRight(EntityType entityType)
		{
			var canSeeFamilies = _userContext.CanAccessProductFamilies();
			var list = new List<TaxonomyMenuItem>();

			if (entityType == EntityType.Project && _userContext.CanActOnProject())
			{
				list.Add(new TaxonomyMenuItem
				{
					Key = "createProject",
					Text = "Create Project",
					Url = Url.PageOrderCreate(null),
					Modal = true,
					CssClass = "primary arrow"
				});
			}

			if (entityType == EntityType.ProjectTemplate && _userContext.CanActOnProjectTemplate())
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


			if (canSeeFamilies && entityType == EntityType.Product)
			{
				ViewBag.Widget = "_ProductFamily";
			}

			if (entityType == EntityType.Product || entityType == EntityType.ProductFamily)
			{
				//if (canSeeFamilies)
				//{
				//	list.Add(new TaxonomyMenuItem() { Key = "uploadProductFamily", Text = "Upload Product Family", Url = Url.PageCreateProductFamily(), Modal = true, CssClass = "arrow" });
				//}
				list.Add(new TaxonomyMenuItem
				{
					Key = "uploadProducts",
					Text = "Add/Edit Products",
					Url = Url.PageCreateProducts(),
					Modal = true,
					CssClass = "primary arrow"
				});
				list.Add(new TaxonomyMenuItem
				{
					Key = "uploadStatus",
					Text = "Check Upload Status",
					Url = Url.PageProductUploads(),
					CssClass = "secondary arrow"
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

		internal class SortListComparer : Comparer<MenuItem>
		{
			private readonly List<string> _sortList;

			public SortListComparer(List<string> sortList)
			{
				_sortList = sortList;
			}

			public override int Compare(MenuItem x, MenuItem y)
			{
				var item1 = x as TaxonomyMenuItem;
				var item2 = y as TaxonomyMenuItem;
				if (item1 == null || item2 == null)
					return 0;

				var ix1 = _sortList.IndexOf(item1.Key);
				var ix2 = _sortList.IndexOf(item2.Key);
				return ix1.CompareTo(ix2);
			}
		}
	}
}