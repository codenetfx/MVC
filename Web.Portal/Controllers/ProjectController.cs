using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.Practices.Unity.Utility;
using UL.Aria.Common.Authorization;
using UL.Aria.Web.Common.Managers.Project;
using UL.Aria.Web.Portal.Logging;
using UL.Enterprise.Foundation;
using UL.Enterprise.Foundation.Framework;
using UL.Enterprise.Foundation.Logging;
using UL.Enterprise.Foundation.Mapper;
using UL.Aria.Service.Contracts.Dto;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Project;
using UL.Aria.Web.Common.Models.Search;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Mvc.Attributes;
using UL.Aria.Web.Common.Providers;
using UL.Aria.Web.Common.Models.History;
using WebGrease.Css.Extensions;
using UL.Aria.Web.Common.Validation.Project;

namespace UL.Aria.Web.Portal.Controllers
{
	/// <summary>
	///     Project pages
	/// </summary>
	[Authorize]
	public sealed class ProjectController : BaseController
	{
        internal const string NoItemsSelectedWarningMessage = "No Order Lines were selected.  Click below or select Order Line to continue.";
	    private readonly IIncomingOrderProvider _incomingOrderProvider;
		private readonly IProjectProvider _projectProvider;
		private readonly ITaskProvider _taskProvider;
		private readonly ISearchProvider _searchProvider;
		private readonly IAriaProvider _ariaProvider;
		private readonly ISessionProvider _sessionProvider;
		private readonly ICompanyProvider _companyProvider;
		private readonly IProductProvider _productProvider;
		private readonly IProjectModelStateValidationStrategy _projectModelStateValidationStrategy;
		private readonly IMapperRegistry _mapperRegistry;
		private readonly IHistoryProvider _historyProvider;
		private readonly IProjectTemplateProvider _projectTemplateProvider;
		private readonly IBusinessUnitProvider _businessUnitProvider;
		private readonly IProjectActionManager _projectActionManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="incomingOrderProvider">The incoming order provider</param>
        /// <param name="projectProvider">The project provider</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="taskProvider">The task provider.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="searchProvider">the search provider</param>
        /// <param name="ariaProvider">The aria provider.</param>
        /// <param name="sessionProvider">The session provider.</param>
        /// <param name="companyProvider">The company provider.</param>
        /// <param name="productProvider">The product provider.</param>
        /// <param name="projectModelStateValidationStrategy">The project model state validation strategy.</param>
        /// <param name="mapperRegistry">The mapper registry.</param>
        /// <param name="historyProvider">The history provider.</param>
        /// <param name="projectTemplateProvider">The ProjectTemplate provider.</param>
        /// <param name="businessUnitProvider">The business unit provider.</param>
        /// <param name="projectActionManager">The project action manager.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
		public ProjectController(IUserContext userContext, IIncomingOrderProvider incomingOrderProvider, IProjectProvider projectProvider, 
            IPortalConfiguration portalConfiguration, ITaskProvider taskProvider, ILogHelper logHelper, ISearchProvider searchProvider, 
            IAriaProvider ariaProvider, ISessionProvider sessionProvider, ICompanyProvider companyProvider, IProductProvider productProvider, 
            IProjectModelStateValidationStrategy projectModelStateValidationStrategy, IMapperRegistry mapperRegistry, IHistoryProvider historyProvider, 
            IProjectTemplateProvider projectTemplateProvider, IBusinessUnitProvider businessUnitProvider, IProjectActionManager projectActionManager,
            ITempDataProviderFactory tempDataProviderFactory)
			: base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
		{
			_incomingOrderProvider = incomingOrderProvider;
			_projectProvider = projectProvider;
			_taskProvider = taskProvider;
			_searchProvider = searchProvider;
			_ariaProvider = ariaProvider;
			_sessionProvider = sessionProvider;
			_companyProvider = companyProvider;
			_productProvider = productProvider;
			_projectModelStateValidationStrategy = projectModelStateValidationStrategy;
			_mapperRegistry = mapperRegistry;
			_historyProvider = historyProvider;
			_projectTemplateProvider = projectTemplateProvider;
			_businessUnitProvider = businessUnitProvider;
			_projectActionManager = projectActionManager;
		}

		/// <summary>
		///     Gets the logging category to use for all logging.
		/// </summary>
		/// <value>
		///     The logging category.
		/// </value>
		protected override LogCategory LoggingCategory
		{
			get { return LogCategory.Project; }
		}

		/// <summary>
		/// Gets the audit history for a project
		/// </summary>
		/// <param name="id">The project guid</param>
		/// <returns></returns>
		[ULEmployee]
		[HttpGet]
		public ActionResult History(Guid id)
		{
			var project = GetProjectOr404(id, false);
			var historyItems = _historyProvider.FetchHistoryByEntityId(id).ToList();

			var model = new ProjectEntityHistoryModel()
			{
				EntityId = id,
				HistoryItems = historyItems.Where(x => !string.IsNullOrEmpty(x.ActionDetail)).OrderBy(x => x.ActionDate).ToList(),
				PageLinks = ProjectSectionNavigationLinks(project.Id, _userContext,Url),
				Breadcrumbs = Breadcrumbs(project, "History"),
				ProjectHandler = project.ProjectHandler,
				Name = project.Name,
				OrderNumber = project.OrderNumber,
				EndDate = project.EndDate,
				IsOnTrack = project.IsOnTrack,
				StartDate = project.StartDate,
				ProjectStatus = project.ProjectStatus,
				PageActions = ProjectActions(project, _userContext,Url),
				CompanyName = project.CompanyName,
                HasAutoComplete = project.HasAutoComplete,
                OverrideAutoComplete = project.OverrideAutoComplete
			};

			ViewBag.TopActions = HistoryPageActions(project);

			return View(model);
		}

		/// <summary>
		/// Exports the history to Excel formated file result.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <returns></returns>
		[ULEmployee]
		public FileStreamResult DownloadHistory(Guid id)
		{
			ContentDownload fileData = _historyProvider.DownloadHistoryByEntityId(id);
			return File(fileData.OutputStream, fileData.ContentType, fileData.FileName);
		}

		/// <summary>
		/// Project overview
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public ActionResult Index(Guid id)
		{
			var model = GetProjectOr404(id, false);

			//
			// populate documents & tasks
			//
			int? productsCount = null, tasksCount = null, documentsCount = null;
			var criteria = new SearchCriteria().ApplyProjectFilter(model.Id).ApplyAssetTypeRefiner().ApplyTaskRefiners();
			var countResults = _searchProvider.Search<ContainerSearchResult>(criteria, _sessionProvider, _userContext);
			List<TaxonomyMenuItem> taskStatusCounts = null;

			List<RefinementItem> assetTypes;
			if (countResults.RefinerResults.TryGetAssetTypeRefiner(out assetTypes))
			{
				productsCount = GetEntityTypeCount(assetTypes, EntityType.Product) ?? 0;
			}

			if (_userContext.CanAccessTasks())
			{
				taskStatusCounts = GetTaskRefiners(countResults, _portalConfiguration, _userContext);
			}
			var documentSearchCriteria =
				new SearchCriteria() { Paging = new Paging() { PageSize = SearchCriteria.MaxPageSize } }.ApplyDocumentSearch()
					.ApplyContainerFilter(model.ContainerId)
					.ApplyProjectFilter(model.Id);
			documentsCount = _searchProvider.FetchDocuments(documentSearchCriteria, _sessionProvider, _userContext).Results.Count;
		    tasksCount = _taskProvider.FetchCountByContainerId(model.ContainerId, _userContext);
            //model.ProjectDocuments = _searchProvider.Search<DocumentSearchResult>(criteria);

			// setup links
			model.PageActions = ProjectActions(model, _userContext, Url);
			model.PageLinks = ProjectSectionNavigationLinks(id, _userContext, Url, tasksCount, documentsCount, productsCount, taskStatusCounts);
			model.Breadcrumbs = Breadcrumbs(model, null);

			return View(model);
		}

		internal static List<TaxonomyMenuItem> GetTaskRefiners(SearchResultSet<ContainerSearchResult> countResults, IPortalConfiguration portalConfiguration, IUserContext user)
		{
			var taskStatusCounts = new List<TaxonomyMenuItem>();

			//
			// add all available Progresses (should we???)
			//
			foreach (var status in portalConfiguration.TaskProgress)
			{
				taskStatusCounts.Add(new TaxonomyMenuItem()
				{
					Text = status.Value,
					Count = 0,
				});
			}

			//
			// update Progress counts (and add new items if missing)
			//
			List<RefinementItem> taskProgress;
			if (countResults.RefinerResults.TryGetTaskProgressRefiner(out taskProgress))
			{
				foreach (var refinementItem in taskProgress)
				{
					var progressValue = refinementItem.Name.ToTaskProgressEnumDto().ToString().ParseOrDefault(default(TaskProgressEnum));
					var displayName = progressValue.GetDisplayName();

					var statusItem = taskStatusCounts.FirstOrDefault(x => x.Text == displayName);
					if (statusItem == null)
					{
						statusItem = new TaxonomyMenuItem() { Text = displayName };
						taskStatusCounts.Add(statusItem);
					}

					statusItem.Count = refinementItem.Count;
					statusItem.Key = refinementItem.Token;
					statusItem.RefinementValue = refinementItem.Value;
				}
			}

			//
			// add the special owner counts (if exists)
			//
			List<RefinementItem> taskOwner;
			if (countResults.RefinerResults.TryGetTaskOwnerRefiner(out taskOwner))
			{
				var loginId = user.LoginId;
				var ownerCount = 0;
				foreach (var owner in taskOwner.Where(x => string.Equals(x.Name, "Unassigned") || string.Equals(x.Name, loginId, StringComparison.OrdinalIgnoreCase)))
				{
					var ownerMenuItem = new TaxonomyMenuItem()
					{
						Text = owner.Name,
						Count = owner.Count,
						Key = owner.Token,
						RefinementValue = owner.Value
					};
					if (string.Equals(ownerMenuItem.Text, loginId, StringComparison.OrdinalIgnoreCase))
						ownerMenuItem.Text = "Me";

					taskStatusCounts.Insert(ownerCount++, ownerMenuItem);
				}
			}

			return taskStatusCounts;
		}

		private void SearchAndBuildModel<TResult>(ProjectDetail project, SearchCriteria criteria, string pageTitle, Func<SearchResultSet<TResult>> searchFunction) where TResult : SearchResult
		{
			criteria.ApplyProjectFilter(project.Id);

			project.PageLinks = ProjectSectionNavigationLinks(project.Id, _userContext, Url);
			project.PageActions = ProjectActions(project, _userContext, Url);

			SearchResultSet<TResult> results;
			results = searchFunction != null ? searchFunction() : _searchProvider.Search<TResult>(criteria, _sessionProvider, _userContext);
			project.SearchResults = results;
			project.SearchResults.SearchCriteria.PrimaryRefiner = _searchProvider.BuildRefiners(project.PageLinks, results.RefinerResults, criteria, _userContext);
			project.Breadcrumbs = Breadcrumbs(project, pageTitle);

			SetSearchMessages(project);
		}

		private void SearchAndBuildModel<TResult>(ProjectDetail project, SearchCriteria criteria, string pageTitle) where TResult : SearchResult
		{
			SearchAndBuildModel(project, criteria, pageTitle, () => _searchProvider.Search<TResult>(criteria, _sessionProvider, _userContext));
		}

		/// <summary>
		/// Products the specified id.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="criteria">The criteria.</param>
		/// <returns></returns>
		public ActionResult Products(Guid id, SearchCriteria criteria)
		{
			var project = GetProjectOr404(id, false);

			criteria.ApplyProductSearch(_userContext);

			SearchAndBuildModel<ProductSearchResult>(project, criteria, "Products");

			return View("Search", project);
		}

		/// <summary>
		/// Documents the specified id.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="criteria">The criteria.</param>
		/// <returns></returns>
		public ActionResult Documents(Guid id, SearchCriteria criteria)
		{
			var project = GetProjectOr404(id, false);

			criteria.ApplyDocumentSearch();
			// SearchAndBuildModel<DocumentSearchResult>(project, criteria, "Documents");

			// JML - use this syntax to perform direct fetch w/out search
			criteria.ApplyContainerFilter(project.ContainerId);
			SearchAndBuildModel<DocumentSearchResult>(project, criteria, "Documents", () => _searchProvider.FetchDocuments(criteria, _sessionProvider, _userContext));

			project.SearchResults.Results.ForEach(x =>
			{
				var r = x as DocumentSearchResult;
				if (r != null)
					r.ApplyProjectPermission(_userContext, project);
			});
			return View("Search", project);
		}


		/// <summary>
		/// Gets the task documents information.
		/// </summary>
		/// <param name="request">The request.</param>
		/// <returns></returns>
		public ActionResult GetTaskDocumentModel(TaskDocumentRequest request)
		{
			var project = GetProjectOr404(request.ProjectId, false);

			if (!request.IsInitialized)
			{
				request.ProjectDocumentCriteria = new SearchCriteria() { Paging = new Paging() { PageSize = SearchCriteria.MaxPageSize } };
				request.ProjectDocumentCriteria.PrimaryRefiner = new TaxonomyMenuItem();
				request.TaskDocumentCriteria = new SearchCriteria() { Paging = new Paging() { PageSize = SearchCriteria.MaxPageSize } };
				request.ProjectDocumentCriteria.PrimaryRefiner = new TaxonomyMenuItem();
				request.ProjectDocumentCriteria.ApplyDocumentSearch();
				request.TaskDocumentCriteria.ApplyDocumentSearch();
				request.ProjectDocumentCriteria.ApplyProjectFilter(project.Id).ApplyContainerFilter(project.ContainerId);
				request.TaskDocumentCriteria.ApplyTaskFilter(request.TaskId).ApplyContainerFilter(request.ContainerId);

				request.IsInitialized = true;
			}
			request.ProjectDocumentCriteria.ApplyContainerFilter(project.ContainerId);
			var projResults = _searchProvider.FetchDocuments(request.ProjectDocumentCriteria, _sessionProvider, _userContext);
			var taskIds = _ariaProvider.FetchParentAssetLinks(request.TaskId);

			return Json(new TaskDocumentsModel()
				{
					Successful = true,
					Request = request,
					ProjectDocumentResults = projResults.Results.Where(x => taskIds.All(y => y != x.Id)).ToList(),
					TaskDocumentResults = projResults.Results.Where(x => taskIds.Any(y => y == x.Id)).ToList()
					//needs default settings because the parameter attribute is using defult settings
				}, new Newtonsoft.Json.JsonSerializerSettings());

		}


		private ProjectDetail GetDetailsModel(Guid id, string pageTitle)
		{
			ViewBag.Title = pageTitle;

			var model = GetProjectOr404(id, false);
			FillCompanyExternalId(model);
			model.PageActions = ProjectActions(model, _userContext, Url);
			model.PageLinks = ProjectSectionNavigationLinks(id, _userContext, Url);
			model.Breadcrumbs = Breadcrumbs(model, pageTitle);
			if (string.IsNullOrWhiteSpace(model.CustomerName))
			{
				model.CustomerName = model.CompanyName;
			}
			if (_userContext.IsUlEmployee)
			{
				var projectDetailsItem = model.PageLinks.Skip(1).First();
				var childMenu = new List<TaxonomyMenuItem> {
                new TaxonomyMenuItem() {Text = "Project Summary", Url = Url.PageProjectDetails(id)},
                new TaxonomyMenuItem() {Text = "Order Information", Url = Url.PageProjectDetailsOrderInfo(id)},
                new TaxonomyMenuItem() {Text = "Customer Information", Url = Url.PageProjectDetailsCustomerInfo(id)},
                new TaxonomyMenuItem() {Text = "Agent Information", Url = Url.PageProjectDetailsAgentInfo(id)},
                new TaxonomyMenuItem() {Text = "Planning & Operations", Url = Url.PageProjectDetailsPlanningAndOperations(id)},
                //new TaxonomyMenuItem() {Text = "Sample Information", Url = Url.PageProjectDetailsSampleInformation(id)}
            };
				projectDetailsItem.AddChild(childMenu);
			}

			return model;
		}

		/// <summary>
		/// Return Project detail view model to General Info view
		/// </summary>
		/// <param name="id">The id.</param>
		/// <returns></returns>
		public ActionResult Details(Guid id)
		{
			if (_userContext.IsUlEmployee)
			{
				var model = GetDetailsModel(id, "Project Details: Project Summary");			
				return View("Detail/ProjectSummary", model);
			}
			else
			{
				var model = GetDetailsModel(id, "Project Details");
				return View("Detail/ProjectInfo", model);
			}
		}
		//***************************************************************//
		//Temporarily disabled. We will enable it later. Don't delete it.


		///// <summary>
		///// Toggles the project visibility.
		///// </summary>
		///// <param name="id">The identifier.</param>
		///// <returns></returns>
		//[ULEmployee]
		//public ActionResult ToggleProjectVisibility(Guid id)
		//{
		//    try
		//    {
		//        var proj = GetProjectEditModel(id);
		//        proj.HideFromCustomer = !proj.HideFromCustomer;
		//        var projToUpdate = _mapperRegistry.Map<ProjectCreate>(proj);
		//        this._projectProvider.Update(projToUpdate, this._userContext);

		//        return Json(new JsonResponseModel()
		//        {
		//            Successful = true,
		//            Message = "Project updated Successfully."
		//        });

		//    }
		//    catch (Exception ex)
		//    {
		//        return Json(new JsonResponseModel()
		//        {
		//            Successful = false,
		//            Message = ex.Message
		//        });
		//    }
		//}

		//****************************************************************//


		/// <summary>
		/// Return Project detail view model to Customer Info view
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[ULEmployee]
		public ActionResult CustomerInfo(Guid id)
		{
			var model = GetDetailsModel(id, "Project Details: Customer Information");
			return View("Detail/CustomerInfo", model);
		}

		/// <summary>
		/// Return Project detail view model to Order Info view
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[ULEmployee]
		public ActionResult OrderInfo(Guid id)
		{
			var model = GetDetailsModel(id, "Project Details: Order Information");
			return View("Detail/OrderInfo", model);
		}

		/// <summary>
		/// Return Project detail view model to Planning and Operations view
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[ULEmployee]
		public ActionResult PlanningAndOperations(Guid id)
		{
			var model = GetDetailsModel(id, "Project Details: Planning & Operations");

			var foo = model.ServiceLineItems;

			return View("Detail/PlanningAndOperations", model);
		}



        /// <summary>
        /// Searches the ul users.
        /// </summary>
        /// <param name="keyword">The keyword.</param>
        /// <returns></returns>
        [ULEmployee]
        public ActionResult SearchCompanies(string keyword)
        {
            var searchCriteria = new SearchCriteria { Query = keyword, Paging = new Paging { Page = 1, PageSize = 1000000, EndIndex = 1000000 } };

            var results = _companyProvider.Search(searchCriteria, _userContext).Results;

            return Json(new JsonResponseModel()
            {
                Successful = true,
                Data = results.Select(x => new
                {
                    Id = x.Id,
                    Display = x.Name,
                    Description = x.Name + "(" + x.ExternalId + ")",
                    Item = x.CompanyId
                }),
                ErrorCode = 200
            });
        }


		/// <summary>
		/// Return Project detail view model to Agent Info view
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[ULEmployee]
		public ActionResult AgentInfo(Guid id)
		{
			var model = GetDetailsModel(id, "Project Details: Agent Information");
			return View("Detail/AgentInfo", model);
		}


		private void PopulateTemplatesAndServiceLines(ProjectCreate model)
		{
			model.ProjectTemplates = _projectTemplateProvider.FetchProjectTemplates(model.BusinessUnitCode);
			model.BusinessUnits = _businessUnitProvider.FetchAll().Select(x =>
			{
				if (x.Name.ToLower() == "all")
				{
					x.Id = Guid.Empty;
				}
				return x;
			}).ToList();

			//
			// fetch order lines either by IncomingOrderId or OrderNumber entered by user
			//
			IncomingOrder order = null;

			if (model.IncomingOrderId.HasValue)
			{
				try
				{
					order = _incomingOrderProvider.Fetch(model.IncomingOrderId.Value);
				}
				catch (Exception exception)
				{
					_logHelper.Log(MessageIds.ProjectControllerServiceLinesRetrieveByIdException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext,
									"Error retrieving service lines using incoming order id " + model.IncomingOrderId.Value, exception);
					ModelState.AddModelError("IncomingOrder", exception.GetBaseException().Message);
				}
			}
			else if (!string.IsNullOrEmpty(model.OrderNumber))
			{
				try
				{
					order = _incomingOrderProvider.FetchByOrderNumber(model.OrderNumber);
				}
				catch (Exception exception)
				{
					_logHelper.Log(MessageIds.ProjectControllerServiceLinesRetrieveByNumberException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext,
									"Error retrieving service lines using order number " + model.OrderNumber, exception);
					ModelState.AddModelError("IncomingOrder", exception.GetBaseException().Message);
				}
			}

			//
			// disallow canceling project if it has associated line items
			//
			if (model.ServiceLines.Count > 0)
			{
				model.AuthorizedProjectStatus = model.AuthorizedProjectStatus.Where(x => x != ProjectStatus.Canceled);
			}

			BuildServiceLineSelectionModel(model, order);
		}

		internal void FillCompanyExternalId(ProjectCreate model)
		{
			if (string.IsNullOrWhiteSpace(model.IncomingOrderCustomerExternalId) && model.CompanyId.HasValue &&
				model.CompanyId.Value != default(Guid))
			{
				var company = _companyProvider.FetchById(model.CompanyId.Value, _userContext);
				if (null != company)
				{
					model.IncomingOrderCustomerExternalId = company.ExternalId;
				}
			}
		}

		internal void FillCompanyExternalId(ProjectDetail model)
		{
			if (string.IsNullOrWhiteSpace(model.IncomingOrderCustomerExternalId) && model.CompanyId.HasValue &&
				model.CompanyId.Value != default(Guid))
			{
				var company = _companyProvider.FetchById(model.CompanyId.Value, _userContext);
				if (null != company)
				{
					model.IncomingOrderCustomerExternalId = company.ExternalId;
				}
			}
		}

		private void BuildServiceLineSelectionModel(ProjectCreate model, IncomingOrder order)
		{
			try
			{
				//
				// add any service lines found with the associated order id/number
				//
				var existingServiceLineCount = model.ServiceLines.Count;
				if (order != null)
				{
					//TODO: consider guarding against associating lines from Requests that do not have a company assigned
					model.ServiceLines.AddRange(order.ServiceLines);
					//ensure that the project matches the order where the lines are coming from
					model.OrderNumber = order.OrderNumber;
					model.IncomingOrderId = order.Id;
					//model.QuoteNo = order.QuoteNo;
					//model.TotalOrderPrice = order.TotalOrderPrice;
					//use the company ID from the order if the project does not have one
					if (model.CompanyId.GetValueOrDefault(Guid.Empty) == Guid.Empty && order.CompanyId.GetValueOrDefault(Guid.Empty) != Guid.Empty)
					{
						model.CompanyId = order.CompanyId;
					}
				}
				if (model.CompanyId.HasValue)
				{
					var company = _companyProvider.FetchById(model.CompanyId.Value, _userContext);
					//model.CompanyName = string.Format("{0}({1})", company.Name, company.ExternalId);
					model.CompanyName = company.Name;
				}
				//
				// build a selection model for service lines and mark the items that
				// already exist on this project as 'selected'
				//
				var i = 0;
				model.SelectedServiceLines = model.ServiceLines.Select(sl => new SelectListItem
				{
					Text = sl.Name,
					Value = sl.Id.ToString(),
					Selected = i++ < existingServiceLineCount
				}).ToList();
			}
			catch (Exception exception)
			{
				_logHelper.Log(MessageIds.ProjectControllerAddOrderNumberException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext,
									 "Error while adding order number " + model.OrderNumber, exception);
				ModelState.AddModelError("IncomingOrder", exception.GetBaseException().Message);
			}
		}

	    private void ValidateCreate(ProjectCreate model)
	    {
		    if (model.IncomingOrderId.HasValue)
		    {
			    if ((!model.ShouldIgnoreOrderLineSelectionValidation.HasValue ||
			         !model.ShouldIgnoreOrderLineSelectionValidation.Value) && null != model.SelectedServiceLines &&
			        model.SelectedServiceLines.All(x => !x.Selected))
			    {
				    ModelState.AddModelError("SelectedServiceLines", NoItemsSelectedWarningMessage);
				    ModelState.Remove("ShouldIgnoreOrderLineSelectionValidation");
				    model.ShouldIgnoreOrderLineSelectionValidation = true;
			    }
			    else
			    {
				    ModelState.Remove("ShouldIgnoreOrderLineSelectionValidation");
				    model.ShouldIgnoreOrderLineSelectionValidation = false;
					var validationResult = _projectProvider.Validate(Guid.NewGuid(), model);
					_projectModelStateValidationStrategy.UpdateModelState(model, ModelState, validationResult);
			    }
		    }
		    else
		    {
				var validationResult = _projectProvider.Validate(Guid.NewGuid(), model);
				_projectModelStateValidationStrategy.UpdateModelState(model, ModelState, validationResult);
		    }

			ValidateOrderNumber(model);
		    ValidateOrderOwner(model);


	    }

		private void ValidateOrderNumber(ProjectCreate model)
		{
			IncomingOrder order = null;
			if (model.IncomingOrderId.HasValue){
			    
				try
				{
					order = _incomingOrderProvider.Fetch(model.IncomingOrderId.Value);
					if (null != order && !string.IsNullOrEmpty(model.OrderNumber) && order.OrderNumber != model.OrderNumber)
						order = _incomingOrderProvider.FetchByOrderNumber(model.OrderNumber);
				}
				catch (Exception exception)
				{
					_logHelper.Log(MessageIds.ProjectControllerValidateOrderNumberServiceLinesRetrieveByIdException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext,
									"Error retrieving service lines using incoming order id " + model.IncomingOrderId.Value, exception);
					ModelState.AddModelError("OrderNumber", exception.GetBaseException().Message);
				}
			}
			else if (!string.IsNullOrEmpty(model.OrderNumber))
			{
				try
				{
					order = _incomingOrderProvider.FetchByOrderNumber(model.OrderNumber);
				}
				catch (Exception exception)
				{
					_logHelper.Log(MessageIds.ProjectControllerValidateOrderNumberServiceLinesRetrieveByNumberException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext,
						"Error retrieving service lines using order number " + model.OrderNumber, exception);
					ModelState.AddModelError("OrderNumber", exception.GetBaseException().Message);
				}
			}

			if (order != null)
			{
				if (order.CompanyId.GetValueOrDefault(Guid.Empty) != Guid.Empty)
				{

					if (model.CompanyId.GetValueOrDefault(Guid.Empty) != Guid.Empty &&
						model.CompanyId != order.CompanyId)
					{
						var orderCompanyName = _companyProvider.FetchDisplayNameById(order.CompanyId.Value);
						ModelState.AddModelError("OrderNumber", string.Format(
							"This Order cannot be associated with this Project because it was created for {0}.  Enter the correct Order Number.",
							orderCompanyName));
					}
				}
			}
		}


		private void ValidateOrderOwner(ProjectCreate project)
		{
			if (project.OrderOwnerAssigned == OrderOwnerAssigned.AssignedToOwner && string.IsNullOrEmpty(project.OrderOwner))
			{
				ModelState.AddModelError("OrderOwner", "OrderOwner is required.");
			}
		}
		#region Project Create and Edit
		/// <summary>
		///     Creates a project from scratch
		/// </summary>
		/// <returns></returns>
		[ULEmployee]
		public ActionResult CreateNew()
		{
			ViewBag.Success = false;
			var model = new ProjectCreate
			{
				AuthorizedProjectStatus = new[] { ProjectStatus.InProgress }, OrderOwnerAssigned = OrderOwnerAssigned.NotApplicable
			};

			PopulateTemplatesAndServiceLines(model);

			return PartialView("Create2", model);
		}

		/// <summary>
		///     Create a project from an existing incoming order request.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		[ULEmployee]
		public ActionResult Create(Guid id)
		{
			try
			{
				var model = new ProjectCreate
				{
					IncomingOrderId = id,
					AuthorizedProjectStatus = new[] { ProjectStatus.InProgress },
					OrderOwnerAssigned = OrderOwnerAssigned.NotApplicable
				};

				ViewBag.Success = false;
				PopulateTemplatesAndServiceLines(model);
				if (model.CompanyId.GetValueOrDefault(Guid.Empty) == Guid.Empty)
				{
					throw new Exception("Company Id is empty");
				}

				return PartialView("create2", model);

			}
			catch (Exception exception)
			{
				var logMessage = string.Format("Creation of project from request {0} has been blocked due to missing company.", id);
				_logHelper.Log(MessageIds.ProjectControllerCreateException, LogCategory.Project, LogPriority.Medium, TraceEventType.Error, HttpContext, logMessage, exception);
				return PartialView("CreateError");
			}
		}

		/// <summary>
		///     Create new project
		/// </summary>
		/// <param name="project"></param> 
		/// <param name="shouldOpenDetailsGroup"></param>
		/// <returns>ActionResult</returns>
		[HttpPost]
		[ULEmployee]
		public ActionResult Create(ProjectCreate project, string shouldOpenDetailsGroup)
		{
			ViewBag.Success = false;
			//var message = new GrowlMessage();

			//if handler is "assigned to me" then ignore errors from the ProjectHandler text box
			if (!project.ProjectHandlerAssigned && ModelState.ContainsKey("ProjectHandler"))
			{
				ModelState["ProjectHandler"].Errors.Clear();
			}

			ValidateCreate(project);
			


			Guid id = Guid.Empty;

			if (ModelState.IsValid)
			{
				try
				{
					id = _incomingOrderProvider.CreateNewProject(project, _userContext);
					ViewBag.Success = true;
					var message = string.Format("Project <a href='{0}'>{1}</a> was successfully created.", Url.PageProjectDetails(id), project.Name);
					AddPageMessage(CreatePageMessage(message, title: "Success!", severity: TraceEventType.Information));
				}
				catch (Exception ex)
				{
					_logHelper.LogError(HttpContext, ex);

					var httpException = ex.GetBaseException() as WebException;
					if (httpException != null && (httpException.Response as HttpWebResponse) !=null && 
						(httpException.Response as HttpWebResponse).StatusCode == HttpStatusCode.NotFound)
					{
						ModelState.AddModelError("",
							"The request no longer exists. A project may have already been created by another user.");
					}
					else
					{
						ModelState.AddModelError("", ex.GetBaseException().Message);
					}
					//message = CreatePageMessage(HttpContext, "There was an error encountered attempting to create the project.", title: "Error", severity: TraceEventType.Error);

					_logHelper.Log(MessageIds.ProjectControllerEditException, LoggingCategory, LogPriority.High,
						TraceEventType.Error, HttpContext, "Error while create a project", ex);

				}
			}

			if (!ViewBag.Success)
			{
				if (project.IncomingOrderId != null)
				{
					PopulateTemplatesAndServiceLines(project);
				}
				else
				{
					//PopulateModelWithCompanies(project);
					if (project.CompanyId.HasValue)
					{
						project.CompanyName = _companyProvider.FetchDisplayNameById(project.CompanyId.Value);
					}
					project.ProjectTemplates = _projectTemplateProvider.FetchProjectTemplates(project.BusinessUnitCode);
					project.BusinessUnits = _businessUnitProvider.FetchAll().Select(x =>
					{
						if (x.Name.ToLower() == "all")
						{
							x.Id = Guid.Empty;
						}
						return x;
					}).ToList();
				}
				project.AuthorizedProjectStatus = new[] { ProjectStatus.InProgress };
			}
			else if (ProjectCreate.shouldOpenDetails == shouldOpenDetailsGroup)
			{        // add location of newly created resource if requested.  

				var action = Url.PageProjectDetails(id);
				if (null == action)
					throw new NullReferenceException();
				Response.Headers.Add("location", action);

				return new EmptyResult();
			}
			else if (null != project.SelectedServiceLines && project.SelectedServiceLines.Count>0 && project.SelectedServiceLines.All(x => x.Selected))
			{
				var action = Url.PageOrderQueue();
				if (null == action)
					throw new NullReferenceException();
				Response.Headers.Add("location", action);

				return new EmptyResult();
			}

			return PartialView("create2", project);
		}

		/// <summary>
		/// Gets the project, converts to ProjectCreate instance, assigns templates and service lines, 
		/// and build page links.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <returns></returns>
		internal ProjectCreate GetProjectEditModel(Guid id)
		{
			var project = GetProjectOr404(id, true);

			var model = _mapperRegistry.Map<ProjectCreate>(project);
			model.ProjectHandlerAssigned = !string.Equals(project.ProjectHandler, _userContext.LoginId, StringComparison.OrdinalIgnoreCase);
			model.OrderOwnerAssigned = string.IsNullOrEmpty(project.OrderOwner)
				? OrderOwnerAssigned.NotApplicable
				: string.Equals(project.OrderOwner, _userContext.LoginId, StringComparison.OrdinalIgnoreCase)
					? OrderOwnerAssigned.AssignedToMe
					: OrderOwnerAssigned.AssignedToOwner;

			PopulateTemplatesAndServiceLines(model);
			// model.PageLinks = EditActions(project);
			model.CurrentUser = _userContext.LoginId;
			return model;
		}

		/// <summary>
		/// Edits the project information.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <returns></returns>
		public ActionResult Edit(Guid id)
		{
			ViewBag.Success = false;
			var model = GetProjectEditModel(id);
			return PartialView("Edit", model);
		}


		/// <summary>
		/// Edit project
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="form">The form.</param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult Edit(Guid id, FormCollection form)
		{
			ViewBag.Success = false;
			ProjectCreate project = GetProjectEditModel(id);
			try
			{
				var originalOrderNumber = project.OrderNumber;
				var originalSelectedServiceLineCount = project.SelectedServiceLines.Count(x => x.Selected);
				TryUpdateModel(project, form);
				if (ModelState.ContainsKey("BusinessUnitCode"))
				{
					ModelState["BusinessUnitCode"].Errors.Clear();
				}
				ValidateOrderNumber(project);
				var orderNumberChanged = !string.Equals(originalOrderNumber, project.OrderNumber);
				if (orderNumberChanged)
				{
					if (originalSelectedServiceLineCount > 0)
						throw new NotSupportedException("Cannot change associated orders after line items have been selected");

					//update lines and order numbers to match new order
					var selected = project.SelectedServiceLines.Where(x => x.Selected).ToList();
					project.IncomingOrderId = null;
					project.ServiceLines.Clear();
					// = _incomingOrderProvider.FetchByOrderNumber(project.OrderNumber).ServiceLines.ToList();

					PopulateTemplatesAndServiceLines(project);

					if (selected.Any() && !project.SelectedServiceLines.Any())
					{
						ModelState.AddModelError("OrderNumber", "");
						throw new NotSupportedException(
						   "The Order Line(s) cannot be associated with this Project and have been removed.");
					}
					else
					{
						foreach (var selectListItem in selected)
						{
							project.SelectedServiceLines.First(x => x.Value == selectListItem.Value).Selected = true;
						}
					}
				}

				if (project.SelectedServiceLines.Any(x => x.Selected))
				{
					ValidateOrderSelection(project);
				}

				var validationResult = _projectProvider.Validate(project.Id, project);
				_projectModelStateValidationStrategy.UpdateModelState(project, ModelState, validationResult);


				//if handler is "assigned to me" then ignore errors from the ProjectHandler text box
				if (!project.ProjectHandlerAssigned)
				{
					project.ProjectHandler = _userContext.LoginId;
					if (ModelState.ContainsKey("ProjectHandler"))
						ModelState["ProjectHandler"].Errors.Clear();
				}

				ValidateOrderOwner(project);

				if (ModelState.IsValid)
				{

					_projectProvider.Update(project, _userContext);
					ViewBag.Success = true;
					var message = string.Format("Project <a href='{0}'>{1}</a> was successfully updated.",
						Url.PageProjectDetails(id), project.Name);
					AddPageMessage(CreatePageMessage(message, title: "Success!", severity: TraceEventType.Information));
				}
			}
			catch (Exception ex)
			{

				var httpException = ex.GetBaseException() as WebException;
				if (httpException != null &&
				    (httpException.Response as HttpWebResponse).StatusCode == HttpStatusCode.NotFound)
				{
					ModelState.AddModelError("",
						"The order line(s) no longer exists. A project may have already been created by another user.");
				}
				else
				{
					ModelState.AddModelError("", ex.GetBaseException().Message);
				}
				_logHelper.Log(MessageIds.ProjectControllerEditException, LoggingCategory, LogPriority.High,
						TraceEventType.Error, HttpContext, "Error editing project", ex);
			}

			return PartialView(project);
		}

		internal void ValidateOrderSelection(ProjectCreate project)
		{
			if (string.IsNullOrWhiteSpace(project.OrderNumber))
			{
				ModelState.AddModelError("OrderNumber", "");
				throw new NotSupportedException(
					"No Order Number is selected, all line item selections will be removed.");
			}

			IncomingOrder order = null;
			try
			{
				order = _incomingOrderProvider.FetchByOrderNumber(project.OrderNumber);
			}
			catch (NullReferenceException)
			{
				return;
			}
			if (order == null)
				return;
			if (!project.CompanyId.HasValue)
			{

				if (order.CompanyId.HasValue && string.IsNullOrWhiteSpace(order.CompanyName))
				{
					var company = _companyProvider.FetchById(order.CompanyId.Value, _userContext);
					if (company != null)
						order.CompanyName = company.Name;
				}

				if (string.Equals(project.CompanyName, order.CompanyName, StringComparison.InvariantCultureIgnoreCase))
				{
					project.CompanyId = order.CompanyId;
				}

			}
			if (project.CompanyId != order.CompanyId)
			{
				var errorMessage = string.Format(
					"The Order Line(s) of this Order Number cannot be associated with this Project because the Order Company Name is " +
					"not the same as the Project Company Name \'{0}\'.", project.CompanyName);
				if (!ModelState.ContainsKey("OrderNumber") || !ModelState["OrderNumber"].Errors.Any())
				{
					ModelState.AddModelError("OrderNumber", errorMessage);
				}
			}
		}


		/// <summary>
		/// Searches for line items to associate to this project
		/// </summary>
		/// <param name="criteria">The criteria.</param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult EditLines(ProjectCreate criteria)
		{
			var project = _projectProvider.Fetch(criteria.Id);
			var order = _incomingOrderProvider.FetchByOrderNumber(string.IsNullOrEmpty(criteria.OrderNumber)? criteria.OrderNumber : criteria.OrderNumber.Trim());

			var model = new ProjectCreate
			{
				OrderNumber = criteria.OrderNumber,
				ServiceLines = project.ServiceLineItems
			};

			BuildServiceLineSelectionModel(model, order);

			return PartialView("_ServiceLinesSearchResults", model);
		}

		#endregion
		/// <summary>
		/// Downloads the product family template.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <returns></returns>
		[ULEmployee]
		public FileStreamResult Download(Guid id)
		{
			ContentDownload fileData = _projectProvider.DownloadProjectsByIds(new List<Guid>() { id });
			return File(fileData.OutputStream, fileData.ContentType, fileData.FileName);
		}


		/// <summary>
		/// Downloads the projects.
		/// </summary>
		/// <returns></returns>
		[ULEmployee]
		public FileStreamResult DownloadProjects()
		{

			var selectedList = _sessionProvider.GetGroupItems(EntityType.Project.ToString());
			if (selectedList == null)
				throw new HttpException((int)HttpStatusCode.NotFound, "No selected project");


			ContentDownload fileData = _projectProvider.DownloadProjectsByIds(selectedList.ToList());
			//foreach (var guid in selectedList)
			//{
			//    _sessionProvider.RemoveGroupItem(guid, EntityType.Project.ToString());
			//}
			return File(fileData.OutputStream, fileData.ContentType, fileData.FileName);
		}

		/// <summary>
		/// Searches the project template.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <returns></returns>
		public JsonResult SearchProjectTemplate(Guid id)
		{
			var result = _projectTemplateProvider.FetchProjectTemplates(id).Select(x => new { id = x.Id, name = x.Name });
			return Json(new
			{
				success = true,
				templates = result
			});
		}

		#region "Product
		/// <summary>
		/// Searches the product.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <returns></returns>
		public ActionResult SearchProduct(Guid id)
		{
			var project = GetProjectOr404(id, false);
			var vm = new SearchCriteria();
			ViewBag.ProductId = id.ToString();
			ViewBag.ProjectName = project.Name;
			return PartialView("AddProduct", vm);
		}

		/// <summary>
		/// Searches the product.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="criteria">The criteria.</param>
		/// <returns></returns>
		[HttpPost]
		public JsonResult SearchProduct(Guid id, SearchCriteria criteria)
		{
			GrowlMessage message = null;
			bool success = false;
			object products = null;

			try
			{
				criteria.EntityType = EntityType.Product;
				criteria.Paging.PageSize = 100;
				var searchResult = _searchProvider.Search<ProductSearchResult>(criteria, _sessionProvider, _userContext);

				if (searchResult.Results.Count == 0)
				{
					message = CreatePageMessage("There was no products found for given search criteria.",
						title: "Information", severity: TraceEventType.Information);
				}
				else
				{
					products = searchResult.Results.Select(x => new { key = x.Id.ToString(), value = x.ModelNumber + "-" + x.Name });
				}
				success = true;
			}

			catch (Exception exception)
			{
				success = false;
				_logHelper.Log(MessageIds.ProjectControllerSearchProductException, LogCategory.Project, LogPriority.High, TraceEventType.Error, this.HttpContext, "there was an error encountered attempting to get products", exception);
				message = CreatePageMessage("There was an error encountered attempting to get products.",
											title: "Error", severity: TraceEventType.Error);
			}


			return Json(new
			{
				success,
				message,
				products
			}, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// Adds the product.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="productId">The product id.</param>
		/// <returns></returns>
		public JsonResult AddProduct(Guid id, string[] productId)
		{
			var message = new GrowlMessage();
			bool success = false;

			var errorCount = 0;

			foreach (var item in productId)
			{

				try
				{
					//ToDo: see if we can get the product names posted back with the ids for performance reasons
					var productDetails = _productProvider.FetchById(item.ToGuid(), _userContext);
					_ariaProvider.LinkProductToProject(id, item.ToGuid(), productDetails.Name, _userContext);
				}
				catch (Exception exception)
				{
					errorCount = errorCount + 1;
					_logHelper.Log(MessageIds.ProjectControllerAddProductException, LogCategory.Project, LogPriority.High, TraceEventType.Error, this.HttpContext, "There was an error encountered attempting to add products.", exception);
				}

			}


			if (errorCount == 0)
			{
				var productDetails = _productProvider.FetchById(productId[0].ToGuid(), _userContext);
				string successMessage = string.Empty;

				if (productId.Count() == 1)
				{
					successMessage = string.Format("<a href='{0}'><strong>{1}</strong> </a> product was added to this project and will be available shortly.", Url.ProductDetails(productId[0].ToGuid()), productDetails.Name);
				}
				else
				{
					successMessage = string.Format("<a href='{0}'><strong>{1}</strong> </a> and {2} other products were added to this project and will be available shortly.", Url.ProductDetails(productId[0].ToGuid()), productDetails.Name, productId.Count() - 1);
				}
				success = true;
				message = CreatePageMessage(successMessage, title: "Products Uploaded", severity: TraceEventType.Information);
			}
			else
			{
				success = false;
				message = CreatePageMessage("There was an error encountered attempting to add products. " + errorCount + " out of  " + productId.Count() + " failed.",
											title: "Error", severity: TraceEventType.Error);
			}
			return Json(new
			{
				success,
				message
			});
		}

		/// <summary>
		/// Removes the product.
		/// </summary>
		/// <param name="productId">The product id.</param>
		/// <param name="projectId">The project id.</param>
		/// <returns></returns>
		public JsonResult RemoveProduct([Bind(Prefix = "id")] Guid productId, Guid projectId)
		{
			var message = new GrowlMessage();
			bool success = false;

			try
			{
				//ToDo: see if we can get the product names posted back with the ids for performance reasons
				var productDetails = _productProvider.FetchById(productId, _userContext);
				_ariaProvider.UnLinkProductToProject(projectId, productId, productDetails.Name, _userContext);
				success = true;
                message = CreatePageMessage("The product was  successfully removed. It may take a minute to be removed from this screen.",
											title: "Success!", severity: TraceEventType.Information);
			}
			catch (Exception exception)
			{
				success = false;
				_logHelper.Log(MessageIds.ProjectControllerRemoveProjectException, LogCategory.Project, LogPriority.High, TraceEventType.Error, this.HttpContext, "There was an error encountered attempting to remove product", exception);
				message = CreatePageMessage("There was an error encountered attempting to remove product.",
											title: "Error", severity: TraceEventType.Error);
			}

			return Json(new
			{
				success,
				message
			});
		}

		#endregion


		#region Internal or Private functions

		internal IEnumerable<Breadcrumb> Breadcrumbs(ProjectDetail project, string crumbText)
		{
			var name = project.Name;
			var pageName = EntityType.Project.GetDisplayName();
			var crumbs = new List<Breadcrumb> { new Breadcrumb { Text = pageName.Pluralize(), Url = Url.PageSearchProjects() } };

			crumbs.Add(new Breadcrumb { Text = name, Url = Url.PageProjectOverview(project.Id) });

			if (crumbText != null)
			{
				crumbs.Add(new Breadcrumb { Text = crumbText, Url = Url.Action(null) });
				ViewBag.SearchTitle = crumbText.Pluralize();
			}

			SetPageMetadata(pageName, crumbs, trackingTitles: new Pair<string, string>(name, project.Id.ToString("N")));

			return ViewBag.BreadCrumbs;
		}

        internal List<TaxonomyMenuItem> HistoryPageActions(ProjectDetail project)
		{
			var id = project.Id;
			var actionsRight = new List<TaxonomyMenuItem>();

			actionsRight.Add(new TaxonomyMenuItem
			{
				Key = "downloadHistory",
				Text = "Export to Excel",
				Url = Url.PageHistoryExportExcel(id),
				CssClass = "arrow primary"
			});
			return actionsRight;
		}

		internal List<TaxonomyMenuItem> ProjectActions(ProjectDetail project, IUserContext userContext, UrlHelper url)
		{
			return _projectActionManager.GetProjectActions(project, userContext, url);
		}

		/// <summary>
		/// Projects the section navigation links.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <param name="userContext">The user context.</param>
		/// <param name="url">The URL.</param>
		/// <param name="taskCount">The task count.</param>
		/// <param name="documentCount">The document count.</param>
		/// <param name="productCount">The product count.</param>
		/// <param name="taskStatus">The task status.</param>
		/// <returns></returns>
		public  static List<TaxonomyMenuItem> ProjectSectionNavigationLinks(Guid id,  IUserContext userContext, UrlHelper url, int? taskCount = null,
			int? documentCount = null, int? productCount = null, IEnumerable<TaxonomyMenuItem> taskStatus = null)
		{
			var menu = new List<TaxonomyMenuItem>();

			menu.Add(new TaxonomyMenuItem()
			{
				Text = "Overview",
				Url = url.PageProjectOverview(id)
			});

			menu.Add(new TaxonomyMenuItem()
			{
				Key = EntityType.Project.ToString(),
				Text = "Project Details",
				IsRefinable = true,
				Url = url.PageProjectDetails(id)
			});

			if (userContext.CanAccessTasks())
			{
				var task = new TaxonomyMenuItem()
				{
					Key = EntityType.Task.ToString(),
					Text = "Tasks",
					IsRefinable = true,
					Url = url.PageProjectTasks(id),
					Count = taskCount
				};

				if (taskStatus != null)
					task.AddChild(taskStatus);

				menu.Add(task);
			}

			menu.Add(new TaxonomyMenuItem()
			{
				Key = EntityType.Document.ToString(),
				Text = "Documents",
				IsRefinable = true,
				Url = url.PageProjectDocuments(id),
				Count = documentCount
			});

			menu.Add(new TaxonomyMenuItem()
			{
				Key = EntityType.Product.ToString(),
				Text = "Products",
				IsRefinable = true,
				Url = url.PageProjectProducts(id),
				Count = productCount
			});

			if (userContext.IsUlEmployee)
			{
				menu.Add(new TaxonomyMenuItem()
				{
					Key = EntityType.Project.ToString(),
					Text = "Project History",
					IsRefinable = false,
					Url = url.PageProjectHistory(id)
				});
			}

			return menu;
		}

		private ProjectDetail GetProjectOr404(Guid id, bool isForEdit)
		{
			var project = _projectProvider.Fetch(id);

			if (project == null)
				throw new HttpException((int)HttpStatusCode.NotFound, "The requested project could not be found");

			if (isForEdit && !_userContext.CanEditProject(project))
				throw new HttpException((int)HttpStatusCode.Forbidden, "This project is Read-Only, it has been Completed or Canceled.");

			//
			// check ACL for rights to view this project
			//
			if (!_userContext.CanAccessProject(project, isForEdit ? SecuredActions.Update : SecuredActions.View))
				throw new HttpException((int)HttpStatusCode.Forbidden, "Not authorized to access this project");

			return project;
		}

		private void SetSearchMessages(ProjectDetail project)
		{
			// to allow our global search result Display Templates to recognize this page
			ViewBag.ActiveProjectId = project.Id;
			SetNoSearchResultsMessage(EntityType.Project);
		}

		#endregion
	}
}