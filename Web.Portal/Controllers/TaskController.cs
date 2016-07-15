using Microsoft.Practices.Unity.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.Practices.EnterpriseLibrary.Common.Utility;
using UL.Aria.Common.Authorization;
using UL.Aria.Service.Contracts.Dto;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Managers.Project;
using UL.Aria.Web.Common.Models.History;
using UL.Aria.Web.Common.Models.Project;
using UL.Aria.Web.Common.Models.Search;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Mvc.Attributes;
using UL.Aria.Web.Common.Providers;
using UL.Aria.Web.Common.Validation.Task;
using UL.Aria.Web.Common.Models.TaskType;
using UL.Aria.Web.Portal.Logging;
using UL.Enterprise.Foundation;
using UL.Enterprise.Foundation.Framework;
using UL.Enterprise.Foundation.Logging;
using UL.Aria.Web.Common.Mvc.JqGrid;
using Newtonsoft.Json;
namespace UL.Aria.Web.Portal.Controllers
{
	/// <summary>
	///     Task pages
	/// </summary>
	[Authorize]
	public sealed class TaskController : BaseController
	{
		private readonly ITaskModelStateValidationStrategy _taskModelStateValidationStrategy;
		private readonly IProjectProvider _projectProvider;
		private readonly ITaskProvider _taskProvider;
		private readonly ISearchProvider _searchProvider;
		private readonly IAriaProvider _ariaProvider;
		private readonly ISessionProvider _sessionProvider;
		private readonly IContainerProvider _containerProvider;
		private readonly IHistoryProvider _historyProvider;
		private readonly IProjectActionManager _projectActionManager;
		private readonly ITaskTypeProvider _taskTypeProvider;
		private readonly IFileTransferProvider _fileTransferProvider;
		private readonly ICertificationManagementProvider _certificationManagementProvider;
		private readonly JsonSerializerSettings _jsonSerializionSettings = null;
        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="taskModelStateValidationStrategy">The task model state validation strategy.</param>
        /// <param name="projectProvider">The project provider</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="taskProvider">The task provider.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="searchProvider">the search provider</param>
        /// <param name="ariaProvider">The aria provider.</param>
        /// <param name="sessionProvider">The session provider.</param>
        /// <param name="containerProvider">The container provider.</param>
        /// <param name="historyProvider">The history provider.</param>
        /// <param name="projectActionManager">The project action manager.</param>
        /// <param name="taskTypeProvider">The task type provider.</param>
        /// <param name="fileTransferProvider">The file transfer provider.</param>
        /// <param name="certificationManagementProvider">The certification management provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
		public TaskController(IUserContext userContext, ITaskModelStateValidationStrategy taskModelStateValidationStrategy, IProjectProvider projectProvider, 
            IPortalConfiguration portalConfiguration, ITaskProvider taskProvider, ILogHelper logHelper, ISearchProvider searchProvider, IAriaProvider ariaProvider, 
            ISessionProvider sessionProvider, IContainerProvider containerProvider, IHistoryProvider historyProvider, IProjectActionManager projectActionManager, 
            ITaskTypeProvider taskTypeProvider, IFileTransferProvider fileTransferProvider, ICertificationManagementProvider certificationManagementProvider,
            ITempDataProviderFactory tempDataProviderFactory)
			: base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
		{
			_taskModelStateValidationStrategy = taskModelStateValidationStrategy;
			_projectProvider = projectProvider;
			_taskProvider = taskProvider;
			_searchProvider = searchProvider;
			_ariaProvider = ariaProvider;
			_sessionProvider = sessionProvider;
			_containerProvider = containerProvider;
			_historyProvider = historyProvider;
			_projectActionManager = projectActionManager;
			_taskTypeProvider = taskTypeProvider;
			_fileTransferProvider = fileTransferProvider;
			_certificationManagementProvider = certificationManagementProvider;
			_jsonSerializionSettings = new JsonSerializerSettings()
			{
				PreserveReferencesHandling = PreserveReferencesHandling.None,
				ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
				TypeNameHandling = TypeNameHandling.None
			};
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
		/// Tasks the specified id.
		/// </summary>
		/// <param name="projectId">The id.</param>
		/// <param name="criteria">The criteria.</param>
		/// <param name="viewType">Type of the view.</param>
		/// <returns></returns>
		[ULEmployee]
		public ActionResult All(Guid projectId, SearchCriteria criteria, TaskViewType viewType = TaskViewType.List)
		{
			var project = GetProjectOr404(projectId, false);
			project.PageLinks = ProjectController.ProjectSectionNavigationLinks(project.Id, _userContext, Url);
			ViewBag.ProjectStatus = project.ProjectStatus;

			ViewBag.GridViewLink = Url.Action("All", new { viewType = TaskViewType.List }) + "&" + criteria.ToQueryString();
			ViewBag.ViewType = viewType;

			criteria.ApplyProjectTaskSearch().ApplyContainerFilter(project.ContainerId);

			// search this project's tasks
			ViewBag.GroupName = GetTaskSelectionName(project.ContainerId);
			var selectedItems = _sessionProvider.GetGroupItems(ViewBag.GroupName);
			SearchResultSet<TaskSearchResult> results = null;

			var isCurrentUserProjectHandler = string.Equals(project.ProjectHandler, _userContext.LoginId, StringComparison.OrdinalIgnoreCase);
			project.CanDeleteTasks = isCurrentUserProjectHandler;

			//if (viewType == "Default")
			//{
			results = _taskProvider.Search(criteria, selectedItems, _userContext);

			// service does not populate container id, so we will since we have it

			_taskProvider.EnrichTaskWithProjectProperties(results.Results, project, _userContext);
			// EnsureProjectProperties(results.Results, project);
			project.SearchResults = results;
			//}


			project.SearchResults.SearchCriteria.PrimaryRefiner = _searchProvider.BuildRefiners(project.PageLinks, results.RefinerResults, criteria, _userContext);
			project.Breadcrumbs = Breadcrumbs(project, "Tasks");

			// to allow our global search result Display Templates to recognize this page
			ViewBag.TopActions = TaskPageActions(project, criteria);

			project.PageActions = ProjectActions(project, _userContext, Url);

			SetSearchMessages(project);

			return View(project);
		}



		/// <summary>
		/// Returns all the task for the specified project.
		/// </summary>
		/// <param name="projectId">The project identifier.</param>
		/// <param name="criteria">The criteria.</param>
		/// <returns></returns>
		[ULEmployee]
		public JsonResult AllProjectTasks(Guid projectId, JsonSearchCriteria criteria)
		{
			var spcriteria = _searchProvider.MapSearchCriteria(criteria);
			// MapJqGridDataToSearchCriteria(spcriteria, postData);
			spcriteria = spcriteria ?? new SearchCriteria();

			var project = GetProjectOr404(projectId, false);
			project.PageLinks = ProjectController.ProjectSectionNavigationLinks(project.Id, _userContext, Url);
			ViewBag.ProjectStatus = project.ProjectStatus;

			spcriteria.ApplyProjectTaskSearch().ApplyContainerFilter(project.ContainerId);

			// search this project's tasks
			ViewBag.GroupName = GetTaskSelectionName(project.ContainerId);
			var selectedItems = _sessionProvider.GetGroupItems(ViewBag.GroupName);

			var results = _taskProvider.FetchProjectTasks(project.Id, spcriteria, _userContext);
			var isCurrentUserProjectHandler = string.Equals(project.ProjectHandler, _userContext.LoginId, StringComparison.OrdinalIgnoreCase);

			project.CanDeleteTasks = (!string.IsNullOrEmpty(project.ProjectHandler) && isCurrentUserProjectHandler);

			// service does not populate container id, so we will since we have it
			// EnsureProjectProperties(results.Results, project);
			_taskProvider.EnrichTaskWithProjectProperties(results.Results, project, _userContext);
			var facadeList = _taskProvider.MapToTaskTreeGridFacade(results.Results, _userContext, Url);

			return Json((new JqGridResults<TaskTreeGridFacade>
			{
				Success = true,
				CurrentPage = spcriteria.Paging.Page,
				CustomDataObject = spcriteria,
				Id = Guid.NewGuid(),
				RepeatItems = true,
				TotalPages = 1,
				TotalRecords = facadeList.Count(),
				Results = facadeList
			}).ToJqGridResult(), _jsonSerializionSettings, JsonRequestBehavior.AllowGet);

		}

		/// <summary>
		/// Saves the bulk.
		/// </summary>
		/// <param name="request">The request.</param>
		/// <returns></returns>
		public JsonResult SaveBulk(JqGridResults<TaskTreeGridFacade> request)
		{
			var validationErrors = new List<ValidationException>();
			var project = GetProjectOr404(request.Results.First().ProjectId, false);

			var errors = _taskProvider.ValidateTasks(project.ContainerId, request.Results);
			validationErrors.AddRange(_taskModelStateValidationStrategy.GetValidationErrors(errors));

			if (validationErrors.Any())
			{
				return Json((new JqGridResults<TaskTreeGridFacade>
				{
					Success = !validationErrors.Any(),
					Id = Guid.NewGuid(),
					RepeatItems = true,
					TotalPages = 1,
					TotalRecords = request.Results.Count(),
					Results = request.Results,
					ValidationErrors = validationErrors
				}).ToJqGridResult(), _jsonSerializionSettings, JsonRequestBehavior.AllowGet);
			}

			IEnumerable<TaskTreeGridFacade> result = null;
			try
			{
				result = _taskProvider.UpdateTasks(project.ContainerId, request.Results, _userContext, Url).ToList();
				result.ForEach(taskFacade => taskFacade.ProjectId = project.Id);
			}
			catch (Exception ex)
			{
				_logHelper.LogError(System.Web.HttpContext.Current, ex);

				return Json((new JqGridResults<TaskTreeGridFacade>
				{
					Success = false,
					//Success =  validationErrors.Any(),
					Id = Guid.NewGuid(),
					RepeatItems = true,
					TotalPages = 1,
					TotalRecords = request.Results.Count(),
					Results = request.Results,
					ValidationErrors = validationErrors
				}).ToJqGridResult(), _jsonSerializionSettings, JsonRequestBehavior.AllowGet);
			}


			return Json((new JqGridResults<TaskTreeGridFacade>
			{
				Success = true,
				Id = Guid.NewGuid(),
				RepeatItems = true,
				TotalPages = 1,
				TotalRecords = result.Count(),
				Results = result
			}).ToJqGridResult(), _jsonSerializionSettings, JsonRequestBehavior.AllowGet);
		}


		/// <summary>
		/// Saves the bulk.
		/// </summary>
		/// <param name="request">The request.</param>
		/// <returns></returns>
		public JsonResult DeleteBulk(JqGridResults<TaskTreeGridFacade> request)
		{
			var validationErrors = new List<ValidationException>();
			var project = GetProjectOr404(request.Results.First().ProjectId, false);

			var errors = _taskProvider.ValidateOnDeleteTasks(project.ContainerId, request.Results.Select(x => x.Id).ToList());
			validationErrors.AddRange(_taskModelStateValidationStrategy.GetDeleteValidationErrorsForGrid(errors));

			if (validationErrors.Any())
			{
				return Json((new JqGridResults<TaskTreeGridFacade>
				{
					Success = !validationErrors.Any(),
					Id = Guid.NewGuid(),
					RepeatItems = true,
					TotalPages = 1,
					TotalRecords = request.Results.Count(),
					Results = request.Results,
					ValidationErrors = validationErrors
				}).ToJqGridResult(), _jsonSerializionSettings, JsonRequestBehavior.AllowGet);
			}

			TaskDeleteResult result = null;
			try
			{
				result = _taskProvider.DeleteTasks(project.ContainerId, request.Results.Select(x => x.Id), _userContext, Url);

			}
			catch (Exception ex)
			{
				_logHelper.LogError(System.Web.HttpContext.Current, ex);

				return Json((new JqGridResults<TaskTreeGridFacade>
				{
					Success = false,
					//Success =  validationErrors.Any(),
					Id = Guid.NewGuid(),
					RepeatItems = true,
					TotalPages = 1,
					TotalRecords = request.Results.Count(),
					Results = request.Results,
					ValidationErrors = validationErrors
				}).ToJqGridResult(), _jsonSerializionSettings, JsonRequestBehavior.AllowGet);
			}

			return Json((new JqGridResults<TaskTreeGridFacade>
			{
				Success = true,
				Id = Guid.NewGuid(),
				RepeatItems = true,
				TotalPages = 1,
				TotalRecords = result.UpdatedTasks.Count(),
				Results = result.UpdatedTasks,
				CustomDataObject = new
				{
					DeletedIds = result.DeletedTaskIds
				}
			}).ToJqGridResult(), _jsonSerializionSettings, JsonRequestBehavior.AllowGet);
		}

		/// <summary>
		/// Prepares view for creating a Certification Request
		/// </summary>
		/// <param name="taskId">The task identifier.</param>
		/// <param name="containerId">The container identifier.</param>
		/// <returns></returns>
		/// <exception cref="System.Web.HttpException">The requested project could not be found</exception>
		[ULEmployee]
		public ActionResult CertificationRequest([Bind(Prefix = "id")] Guid taskId, Guid? containerId = null)
		{
			if (containerId == null)
				containerId = _searchProvider.GetContainerId(taskId, _sessionProvider, _userContext);

			if (containerId == null)
				throw new HttpException((int)HttpStatusCode.NotFound, "The requested project could not be found");

			var container = _containerProvider.FetchById(containerId.Value);
			var project = _projectProvider.Fetch(container.PrimarySearchEntityId);
			var model = _certificationManagementProvider.FetchOrCreateEditModel(taskId);
			model.ProjectId = project.Id;
			if (string.IsNullOrEmpty(project.CCN)
				|| string.IsNullOrEmpty(project.FileNo)
				|| string.IsNullOrEmpty(project.OrderNumber)
				|| string.IsNullOrEmpty(project.ProjectNumber)
				)
			{
				return PartialView("CertificationRequestMissingProjectInfo", project);
			}

			model.ContainerId = containerId.Value;
			model.DepartmentList = _portalConfiguration.Departments.Select(x => new SelectListItem { Text = x.Value, Value = x.Key }).ToList();
			return PartialView(model);
		}

		/// <summary>
		/// Submits a certification request.
		/// </summary>
		/// <param name="certificationManagement">The certification management.</param>
		/// <returns></returns>
		/// <exception cref="System.Web.HttpException">The requested project could not be found</exception>
		[HttpPost]
		[ULEmployee]
		public ActionResult CertificationRequest(CertificationManagement certificationManagement)
		{
			if (ModelState.IsValid)
			{
				try
				{
					certificationManagement.Id = Guid.NewGuid();
					_certificationManagementProvider.PublishCertificationRequest(certificationManagement, _userContext);
					certificationManagement.Success = true;
					AddPageMessage(CreatePageMessage("The CO Request has been submitted.", false, TraceEventType.Information, "Success!"));
				}
				catch (Exception ex)
				{
					certificationManagement.Success = false;
					_logHelper.LogError(this.HttpContext, ex);
					ModelState.AddModelError("", ex.GetBaseException().Message);
					AddPageMessage(CreatePageMessage("There has been an error while submitting CO request"
						+ ex.GetBaseException().Message, true, TraceEventType.Information, "Error!"));
				}
			}
			if (!certificationManagement.Success)
			{
				certificationManagement.DepartmentList =
				_portalConfiguration.Departments.Select(x => new SelectListItem { Text = x.Value, Value = x.Key }).ToList();
				certificationManagement.DapProjectList = CertificationManagement.GetDapProjectList();
			}
			return PartialView(certificationManagement);
		}

		/// <summary>
		/// Tasks the detail.
		/// </summary>
		/// <param name="taskId">The task identifier.</param>
		/// <param name="containerId">The container identifier.</param>
		/// <returns></returns>
		/// <exception cref="System.Web.HttpException">
		/// The requested project could not be found
		/// or
		/// The requested task could not be found
		/// </exception>
		[ULEmployee]
		public ActionResult Detail([Bind(Prefix = "id")] Guid taskId, Guid? containerId = null)
		{
			if (containerId == null)
				containerId = _searchProvider.GetContainerId(taskId, _sessionProvider, _userContext);

			if (containerId == null)
				throw new HttpException((int)HttpStatusCode.NotFound, "The requested project could not be found");

			var container = _containerProvider.FetchById(containerId.Value);
			var project = _projectProvider.Fetch(container.PrimarySearchEntityId);

			var task = _taskProvider.FetchById(project.ContainerId, taskId, _userContext);
			if (task == null)
				throw new HttpException((int)HttpStatusCode.NotFound, "The requested Task could not be found");

			if (task.TaskOwnerAssigned == EnumTaskOwner.AssignToGroup)
			{
				task.TaskOwner = _portalConfiguration.TaskReviewGroupEmail;
			}
			else if (task.TaskOwnerAssigned == EnumTaskOwner.AssignToMe)
			{
				task.TaskOwner = _userContext.LoginId;
			}
			task.ContainerId = containerId;
			var documentSearchCriteria = new SearchCriteria() { Paging = new Paging() { PageSize = SearchCriteria.MaxPageSize } };

			documentSearchCriteria.ApplyDocumentSearch()
				.ApplyContainerFilter(project.ContainerId)
				.ApplyProjectFilter(project.Id);

			var projDocuments = _searchProvider.FetchDocuments(documentSearchCriteria, _sessionProvider, _userContext);
			var taskIds = _ariaProvider.FetchParentAssetLinks(taskId);

			var taskDocumentResults = projDocuments.Results.Where(x => taskIds.Any(y => y == x.Id)).ToList();

			var certificationManagementRequests = _certificationManagementProvider.FetchCertificationRequests(taskId)
				.OrderByDescending(x => x.CreatedDateTime);

			var taskType = _taskTypeProvider.FetchNonEditableById(task.TaskTypeId.GetValueOrDefault());

			var model = new TaskInfo()
			{
				Task = task,
				Documents = taskDocumentResults,
				ProjectHandler = project.ProjectHandler,
				Name = project.Name,
				OrderNumber = project.OrderNumber,
				EndDate = project.EndDate,
				IsOnTrack = project.IsOnTrack,
				StartDate = project.StartDate,
				ProjectStatus = project.ProjectStatus,
				CompanyName = project.CompanyName,
				TaskType = taskType,
				ContainerId = containerId.Value,
				CertificationManagementRequests = certificationManagementRequests,
				Notifications = task.Notifications,
				FreeformTaskTypeId = task.FreeformTaskTypeId
			};
			ViewBag.Title = "Task Details";

			if (project.ProjectStatus != ProjectStatus.Completed && project.ProjectStatus != ProjectStatus.Canceled)
			{
				model.PageActions = TaskDetailActions(task, project.Id, task.ContainerId.Value, taskType);
			}

			model.PageLinks = TaskNavigationLinks(taskId, project);
			model.Breadcrumbs = TaskBreadcrumbs(project, task, "Task Details");
			return View(model);
		}

		/// <summary>
		/// Exports the tasks.
		/// </summary>
		/// <param name="projectId">The id.</param>
		/// <param name="containerId">The container id.</param>
		/// <param name="searchCriteria">The search criteria.</param>
		/// <returns></returns>
		[ULEmployee]
		public FileStreamResult DownloadTasks(Guid projectId, Guid containerId, SearchCriteria searchCriteria)
		{
			searchCriteria.ApplyProjectTaskSearch().ApplyContainerFilter(containerId);

			var fileData = _taskProvider.DownloadSearchByContainerId(searchCriteria);

			return File(fileData.OutputStream, fileData.ContentType, fileData.FileName);
		}

		/// <summary>
		/// Edits the task.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="containerId">The container id.</param>
		/// <param name="isReactivateReqeust">if set to <c>true</c> [is reactivate reqeust].</param>
		/// <param name="disableParentTask">if set to <c>true</c> [disable parent task].</param>
		/// <returns></returns>
		/// <exception cref="System.Web.HttpException">Project not found</exception>
		[ULEmployee]
		public ActionResult Edit(Guid id, Guid? containerId = null, bool isReactivateReqeust = false, bool disableParentTask = false)
		{
			if (containerId == null)
				containerId = _searchProvider.GetContainerId(id, _sessionProvider, _userContext);

			if (containerId == null)
				throw new HttpException((int)HttpStatusCode.NotFound, "The requested project could not be found");
			var container = _containerProvider.FetchById(containerId.Value);
			var project = _projectProvider.Fetch(container.PrimarySearchEntityId);
			var vm = _taskProvider.FetchById(containerId.Value, id, _userContext);

			if (vm == null)
				throw new HttpException((int)HttpStatusCode.NotFound, "The requested task could not be found");

			vm = _taskProvider.FetchStatusListByTaskData(containerId.Value, id, vm, _userContext);
			vm.IsReactivateRequest = isReactivateReqeust;
			if (isReactivateReqeust)
			{
				if (vm.ParentTaskNumber.HasValue)
				{
					var parentTask = _taskProvider.FetchByTaskNumber(containerId.Value, vm.ParentTaskNumber.Value, _userContext);
					if (parentTask.Phase == TaskStatusEnum.Completed || parentTask.Phase == TaskStatusEnum.Canceled)
					{
						vm.ParentTaskStatus = parentTask.Phase;
						return PartialView("ReActivateTask", vm);
					}

				}
				vm.DisableParentTask = disableParentTask;
				vm.Phase = TaskStatusEnum.InProgress;
			}

			EnsureTaskUpdatedWithProjectDetails(vm, project);
			SetTaskDetailViewDependencies(vm);

			return PartialView(vm);
		}

		private void EnsureTaskUpdatedWithProjectDetails(TaskDetail vm, ProjectDetail project)
		{
			vm.ProjectName = project.Name;
			vm.ProjectId = project.Id;
			vm.ProjectHandler = project.ProjectHandler;
			vm.CurrentUser = _userContext.LoginId;
		}

		/// <summary>
		/// Edits the task.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="taskDetail">The task detail.</param>
		/// <param name="taskDocuments">The task documents.</param>
		/// <returns></returns>
		[HttpPost]
		[ULEmployee]
		public ActionResult Edit(Guid id, TaskDetail taskDetail, [JSONDeserialize]TaskDocumentChangeRequest taskDocuments = null)
		{
			try
			{
				taskDetail = _taskProvider.GetTaskDetailModel(taskDetail, _userContext);
				EnsureContainerId(id, taskDetail);
				if (taskDetail.IsReactivateRequest)
				{
					ValidateTaskOwner(taskDetail);
				}

				if (ModelState.IsValid)
					taskDetail = ValidateTask(id, taskDetail);

				if (ModelState.IsValid)
				{
					AddDocumentsToTask(taskDetail, taskDocuments);
					_taskProvider.Update(id, taskDetail);
					taskDetail.Success = true;
					var message = string.Format("Task <strong>{0}</strong> has been successfully updated.  It may take a minute before your changes are reflected on the site.", taskDetail.TaskName);

					AddPageMessage(CreatePageMessage(message, false, TraceEventType.Information, "Success!"));
				}
				else
				{
					taskDetail.Success = false;

				}
			}
			catch (Exception)
			{
				taskDetail.Success = false;
				taskDetail.ErrorMessage = "There was an error encountered attempting to update the task.";
			}

			if (!taskDetail.Success)
			{
				SetTaskDetailViewDependencies(taskDetail);
				taskDetail = _taskProvider.FetchStatusListByTaskData(taskDetail.ContainerId.Value, id, taskDetail, _userContext, true);
				var container = _containerProvider.FetchById(taskDetail.ContainerId.Value);
				var project = _projectProvider.Fetch(container.PrimarySearchEntityId);
				EnsureTaskUpdatedWithProjectDetails(taskDetail, project);
			}

			return PartialView(taskDetail);
		}


		/// <summary>
		/// Removes the parent association.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <param name="containerId">The container identifier.</param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult RemoveParentAssociation(Guid id, Guid containerId)
		{
			try
			{
				_taskProvider.RemoveParentAssociation(id, containerId);
				return RedirectToAction("Edit", new { id = id, containerId = containerId, isReactivateReqeust = true, disableParentTask = true });

			}
			catch (Exception exception)
			{
				var errorMessage = "There was an error while removing parent task association. <br/>" +
								   exception.GetBaseException().Message;

				_logHelper.Log(MessageIds.ProjectControllerTaskCreateException, LogCategory.Project, LogPriority.High, TraceEventType.Error, HttpContext, "There was an error while removing parent task association", exception);

				return PartialView("ReActivateTask", new TaskDetail { TaskId = id, ContainerId = containerId, ErrorMessage = errorMessage });
			}

		}

		private void ValidateTaskOwner(TaskDetail taskDetail)
		{
			if (taskDetail.TaskOwnerAssigned == EnumTaskOwner.AssignToHandler && string.IsNullOrEmpty(taskDetail.TaskOwner))
			{
				ModelState.AddModelError("TaskOwner", "TaskOwner is required.");
			}

		    var requiresStartDate = (null == taskDetail.PredecessorTaskList || taskDetail.PredecessorTaskList.Count() == 0)
		                            && (!taskDetail.StartDate.HasValue || taskDetail.StartDate.Value == default(DateTime));
			if (requiresStartDate)
			{
				ModelState.AddModelError("StartDate", "StartDate is required.");
			}
		}

		/// <summary>
		/// Deletes the task group.
		/// </summary>
		/// <param name="id">The container id.</param>
		/// <returns></returns>
		[HttpPost]
		[ULEmployee]
		public ActionResult DeleteTaskGroup(Guid? id)
		{
			var groupKey = string.Empty;

			if (id == null || id.GetValueOrDefault() == Guid.Empty)
				groupKey = EntityType.Task.ToString();
			else
				groupKey = GetTaskSelectionName(id.Value);

			var hashSetIds = _sessionProvider.GetGroupItems(groupKey);
			var response = DeleteTask(hashSetIds.ToArray(), id);

			return response;
		}

		/// <summary>
		/// Deletes the task.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="containerId">The container id.</param>
		/// <returns></returns>
		[ULEmployee]
		public JsonResult DeleteTask(Guid[] id, Guid? containerId)
		{
			GrowlMessage message = null;
			bool success = true;
			if (containerId.GetValueOrDefault() == Guid.Empty)
			{
				containerId = _searchProvider.GetContainerId(id[0], _sessionProvider, _userContext);
			}
			var validationErrors = _taskProvider.ValidateOnDeleteTasks(containerId.GetValueOrDefault(), new List<Guid>(id));

			if (validationErrors != null && validationErrors.Any())
			{
				var messages = _taskModelStateValidationStrategy.GetDeleteValidationErrors(validationErrors);


				message = CreatePageMessage(string.Format("There was an error deleting task(s). {0}", string.Join("<br/>", messages)),
						title: "Error",
						severity: TraceEventType.Error, sticky: true);
			}
			else
			{
				try
				{
					_taskProvider.DeleteTasks(containerId.GetValueOrDefault(), id, _userContext, Url);

					message =
						CreatePageMessage(
							"Task(s) have been deleted successfully.  It may take a minute before your changes are reflected on the site.",
							title: "Success!", severity: TraceEventType.Start);

					
				}
				catch (Exception ex)
				{
					_logHelper.Log(MessageIds.ProjectControllerTaskDeleteException, LogCategory.Project, LogPriority.High,
				TraceEventType.Error, ControllerContext.HttpContext, ex.Message, ex);
					message = CreatePageMessage(string.Format("There was an error deleting task(s)."), title: "Error",
						   severity: TraceEventType.Error, sticky: true);

				}
			}
			AddPageMessage(message);
			id.ForEach(guid =>
			{
				_sessionProvider.RemoveGroupItem(guid, EntityType.Task.ToString());
				_sessionProvider.RemoveGroupItem(guid, GetTaskSelectionName(containerId.GetValueOrDefault()));
			});


			return Json(new
			{
				success,
				message
			});
		}

		/// <summary>
		/// Creates the task.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="containerId">The container id.</param>
		/// <returns></returns>
		[ULEmployee]
		public ActionResult Create(Guid id, Guid? containerId)
		{
			var vm = new TaskDetail() { ContainerId = containerId, Progress = TaskProgressEnum.OnTrack };
			SetTaskDetailViewDependencies(vm);
			vm.TaskTypes =
				_taskTypeProvider.FetchAll()
					.OrderBy(x => x.Name)
					.ToList();
			vm.IsCreate = true;

			if (containerId.HasValue)
			{
				var container = _containerProvider.FetchById(containerId.Value);
				var project = _projectProvider.Fetch(container.PrimarySearchEntityId);
				EnsureTaskUpdatedWithProjectDetails(vm, project);
			}

			vm = _taskProvider.FetchStatusListOnCreate(Guid.NewGuid(), Guid.NewGuid(), vm);
			return PartialView(vm);
		}

		/// <summary>
		/// Creates the task.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="taskDetail">The task detail.</param>
		/// <returns></returns>
		/// <exception cref="System.Exception">Invalid model</exception>
		[HttpPost]
		[ULEmployee]
		public ActionResult Create(Guid id, TaskDetail taskDetail)
		{
			try
			{
				//taskDetail = BuildTaskDetailModel(taskDetail);
				taskDetail = _taskProvider.GetTaskDetailModel(taskDetail, _userContext);
				EnsureContainerId(id, taskDetail);
                taskDetail.TaskId = Guid.NewGuid();

				//First validate model  and if it is good then validate subtask
				if (ModelState.IsValid)
                    taskDetail = ValidateTask(taskDetail.TaskId.Value, taskDetail);

				if (ModelState.IsValid)
				{
					_taskProvider.Create(taskDetail);
					taskDetail.Success = true;
					var message = string.Format("Task <strong>{0}</strong> has been successfully created.", taskDetail.TaskName);
					AddPageMessage(CreatePageMessage(message, title: "Success!"));
				}
				else
				{
					taskDetail.Success = false;

				}
			}
			catch (Exception ex)
			{
				_logHelper.Log(MessageIds.ProjectControllerTaskCreateException, LogCategory.Project, LogPriority.High, TraceEventType.Error, HttpContext, "Error creating task", ex);
				taskDetail.Success = false;
				taskDetail.ErrorMessage = "There was an error encountered attempting to create the task.  " + ex.GetBaseException().Message;
			}

			if (!taskDetail.Success)
			{
				SetTaskDetailViewDependencies(taskDetail);
				taskDetail = _taskProvider.FetchStatusListOnCreate(taskDetail.ContainerId.Value, Guid.NewGuid(), taskDetail);
				var container = _containerProvider.FetchById(taskDetail.ContainerId.Value);
				var project = _projectProvider.Fetch(container.PrimarySearchEntityId);
				EnsureTaskUpdatedWithProjectDetails(taskDetail, project);
			}

			return PartialView(taskDetail);
		}

		/// <summary>
		/// Gets the audit history for a task
		/// </summary>
		/// <param name="id">The task guid</param>
		/// <param name="projectId">The project guid</param>
		/// <returns></returns>
		[ULEmployee]
		[HttpGet]
		public ActionResult History(Guid id, Guid projectId)
		{
			var project = GetProjectOr404(projectId, false);
			var task = _taskProvider.FetchById(project.ContainerId, id, _userContext);
			if (task == null)
				throw new HttpException((int)HttpStatusCode.NotFound, "The requested Task could not be found");

			var historyItems = _historyProvider.FetchTaskHistory(project.ContainerId, id).ToList();

			var model = new ProjectEntityHistoryModel()
			{
				EntityId = id,
				HistoryItems = historyItems.OrderBy(x => x.ActionDate).ToList(),
				PageLinks = TaskNavigationLinks(id, project),
				Breadcrumbs = TaskBreadcrumbs(project, task, "History"),
				ProjectHandler = project.ProjectHandler,
				Name = project.Name,
				OrderNumber = project.OrderNumber,
				EndDate = project.EndDate,
				IsOnTrack = project.IsOnTrack,
				StartDate = project.StartDate,
				ProjectStatus = project.ProjectStatus,
				CompanyName = project.CompanyName
			};

			if (project.ProjectStatus != ProjectStatus.Completed && project.ProjectStatus != ProjectStatus.Canceled)
			{
				model.PageActions = TaskDetailActions(task, project.Id, task.ContainerId.Value, null);
			}

			ViewBag.TopActions = TaskHistoryPageActions(id, project);

			return View(model);
		}

		/// <summary>
		/// Exports the history to Excel formatted file result.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="containerId">The container identifier.</param>
		/// <returns></returns>
		[ULEmployee]
		public FileStreamResult DownloadHistory(Guid id, Guid containerId)
		{
			var fileData = _historyProvider.DownloadTaskHistory(id, containerId);
			return File(fileData.OutputStream, fileData.ContentType, fileData.FileName);
		}


		/// <summary>
		/// Gets the task type behaviors.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <returns></returns>
		public JsonResult GetTaskTypeBehaviors(Guid id)
		{
			var readonlyFields = GetReadyOnlyFieldsByTaskType(id);
			return Json(new
			{
				success = true,
				readonlyFields = readonlyFields
			});
		}

		private IEnumerable<TaskReadonlyField> GetReadyOnlyFieldsByTaskType(Guid id)
		{
			var readonlyFields = new List<TaskReadonlyField>();
			var taskType = _taskTypeProvider.FetchById(id);
			if (taskType != null)
			{
				foreach (var behavior in taskType.Behaviors)
				{
					var fieldName = Constants.TaskReadonlyFieldsReplacements.ContainsKey(behavior.FieldName)
						? Constants.TaskReadonlyFieldsReplacements[behavior.FieldName]
						: behavior.FieldName;

					readonlyFields.Add(new TaskReadonlyField() { FieldName = fieldName, TitleDesc = "Edit is restricted to the Project Handler" });
					if (fieldName == "TaskOwner")
					{
						readonlyFields.Add(new TaskReadonlyField() { FieldName = "TaskOwnerAssigned", TitleDesc = "Edit is restricted to the Project Handler" });
					}
				}
			}
			return readonlyFields;
		}
		/// <summary>
		/// Creates the document.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <param name="containerId">The container identifier.</param>
		/// <param name="taskId">The task identifier.</param>
		/// <returns></returns>
		[ULEmployee]
		[HttpGet]
		public ActionResult CreateDocumentFromTemplate(Guid id, Guid containerId, Guid taskId)
		{
			var model = new TaskDocument
			{
				AssetId = id,
				ContainerId = containerId,
				TaskId = taskId,
				Permission = DocumentPermission.Private
			};
			EnrichTaskDocumentModel(model);
			return PartialView("CreateDocument", model);
		}

		/// <summary>
		/// Creates the document.
		/// </summary>
		/// <param name="taskDocument">The task document.</param>
		/// <returns></returns>
		[ULEmployee]
		[HttpPost]
		public ActionResult CreateDocumentFromTemplate(TaskDocument taskDocument)
		{
			try
			{
				var doc = _fileTransferProvider.CheckIsDocumentExists(taskDocument);
				if (doc != null)
				{
					if (taskDocument.Overwrite)
					{
						taskDocument.AssetId = doc.AssetId;
					}
					else
					{
						ModelState.AddModelError("Title", "A File Already Exists with that Title.");
					}
				}

				if (ModelState.IsValid)
				{
					taskDocument.LastModifiedBy = _userContext.LoginId;
					taskDocument = _taskProvider.CreateDocumentFromTemplate(taskDocument);
					var msg = CreatePageMessage("Document has been successfully created.", title: "Create Document from Template");
					AddPageMessage(msg);
					taskDocument.Success = true;
				}
			}
			catch (Exception ex)
			{
				_logHelper.Log(MessageIds.TaskCreateDocumentException, LogCategory.PortalPage, LogPriority.High, TraceEventType.Error, HttpContext, "Error while adding a document from predefined task template", ex);
				taskDocument.Success = false; ;
				ModelState.AddModelError("", "Error while adding a document from predefined task template. " + ex.GetBaseException().Message);
			}

			if (!taskDocument.Success)
				EnrichTaskDocumentModel(taskDocument);

			return PartialView("CreateDocument", taskDocument);
		}

		private void EnrichTaskDocumentModel(TaskDocument model)
		{
			model.DocumentTypes = DocumentMetadataUpload.AuthorizedDocumentListItems(_portalConfiguration.DocumentTypes);

			var documentPermissions = EnumUtility.GetValues<DocumentPermission>().Skip(1).Select(x => new SelectListItem
			{
				Text = x.GetDisplayName(),
				Value = x.ToString()
			});
			model.Permissions = new SelectList(documentPermissions, "Value", "Text");
		}


		/// <summary>
		/// Adds the product.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="documentIds">The product id.</param>
		/// <returns></returns>
		public JsonResult AddDocumentToTask(Guid id, string[] documentIds)
		{
			var message = new GrowlMessage();
			bool success = false;

			var errorCount = 0;

			foreach (var documentId in documentIds)
			{
				try
				{
					//ToDo: see if we can get the document titles posted back with the ids for performance reasons
					var documentMetadata = _ariaProvider.FetchMetadata(EntityType.Document, AssetType.Document, documentIds.First().ToGuid());
					var docTitle = (null != documentMetadata && documentMetadata.ContainsKey(AssetFieldNames.AriaTitle)) ? documentMetadata[AssetFieldNames.AriaTitle] : documentId;
					_ariaProvider.LinkDocumentToTask(id, documentId.ToGuid(), docTitle, _userContext);
				}
				catch (Exception exception)
				{
					errorCount = errorCount + 1;
					_logHelper.Log(MessageIds.ProjectControllerAddDocumentToTaskException, LogCategory.Project, LogPriority.High, TraceEventType.Error, this.HttpContext, "There was an error encountered attempting to add documents.", exception);
				}
			}

			if (errorCount == 0)
			{
				var criteria = new SearchCriteria();

				var successMessage = string.Empty;
				if (documentIds.Count() == 1)
				{
					var documentMetadata = _ariaProvider.FetchMetadata(EntityType.Document, AssetType.Document, documentIds.First().ToGuid());

					var docName = (null != documentMetadata && documentMetadata.ContainsKey(AssetFieldNames.AriaName)) ? documentMetadata[AssetFieldNames.AriaName] : documentIds[0].ToString();
					successMessage = string.Format("<a href='{0}'><strong>{1}</strong> </a> product was added to this project and will be available shortly.", Url.ProductDetails(documentIds[0].ToGuid()), docName);
				}
				else
				{
					successMessage = string.Format("<a href='{0}'><strong>{1}</strong> </a> and {2} other products were added to this project and will be available shortly.", Url.ProductDetails(documentIds[0].ToGuid()), documentIds[0], documentIds[0].Count() - 1);
				}

				success = true;
				message = CreatePageMessage(successMessage, title: "Documents Uploaded", severity: TraceEventType.Information);
			}
			else
			{
				success = false;
				message = CreatePageMessage("There was an error encountered attempting to add products. " + errorCount + " out of  " + documentIds + " failed.",
											title: "Error", severity: TraceEventType.Error);
			}
			return Json(new
			{
				success,
				message
			});
		}

		/// <summary>
		/// Assigns the task group.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <returns></returns>
		[HttpGet]
		public ActionResult AssignTaskGroup(Guid? id)
		{
			return PartialView("_AssignTaskGroup", new AssignTaskGroup());
		}

		/// <summary>
		/// Assigns the task group.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <param name="taskGroup">The task group.</param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult AssignTaskGroup(Guid? id, AssignTaskGroup taskGroup)
		{
			if (ModelState.IsValid)
			{
				var groupKey = string.Empty;

				if (id == null || id.GetValueOrDefault() == Guid.Empty)
					groupKey = EntityType.Task.ToString();
				else
					groupKey = GetTaskSelectionName(id.Value);

				var hashSetIds = _sessionProvider.GetGroupItems(groupKey);

				var response = AssignTask(hashSetIds.ToArray(), id, taskGroup);
				ViewBag.Success = true;
				return response;
			}
			else
			{
				return PartialView("_AssignTaskGroup", taskGroup);
			}
		}

		/// <summary>
		/// Assigns the task.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <param name="containerId">The container identifier.</param>
		/// <param name="taskGroup">The task group.</param>
		/// <returns></returns>
		private ActionResult AssignTask(Guid[] id, Guid? containerId, AssignTaskGroup taskGroup)
		{
			int errorCount = 0;
			if (id.Length > 0)
			{
				foreach (var guid in id)
				{
					try
					{
						if (containerId.GetValueOrDefault() == Guid.Empty)
						{
							containerId = _searchProvider.GetContainerId(guid, _sessionProvider, _userContext);
						}

						var task = _taskProvider.FetchById(containerId.GetValueOrDefault(), guid, _userContext);
						if (task == null)
							throw new HttpException((int)HttpStatusCode.NotFound, "Task couldn't be found.");

						task.TaskOwner = taskGroup.TaskOwnerAssigned == EnumTaskOwner.AssignToMe
							? _userContext.LoginId
							: taskGroup.TaskOwner;

						task.ModifiedBy = _userContext.LoginId;
						task.TaskOwnerAssigned = taskGroup.TaskOwnerAssigned;

						_taskProvider.Update(guid, task);
					}
					catch (Exception exception)
					{
						// If Item is already deleted we are ignoring it. Middle tier is not handling it.
						var ex = exception.GetBaseException() as WebException;
						var isDeletedTask = ex != null && ex.Response as HttpWebResponse != null &&
											(ex.Response as HttpWebResponse).StatusCode == HttpStatusCode.NotFound;

						if (!isDeletedTask)
						{
							errorCount++;
							_logHelper.Log(MessageIds.ProjectControllerTaskDeleteException, LogCategory.Project, LogPriority.High,
								TraceEventType.Error, ControllerContext.HttpContext, exception.Message, exception);
						}
					}

					finally
					{
						_sessionProvider.RemoveGroupItem(guid, EntityType.Task.ToString());
						_sessionProvider.RemoveGroupItem(guid, GetTaskSelectionName(containerId.Value));
					}
				}
			}

			if (errorCount == 0)
			{
				AddPageMessage("Task(s) have been assigned successfully.  It may take a minute before your changes are reflected on the site.", title: "Success!", severity: TraceEventType.Start);
			}
			else
			{
				AddPageMessage(string.Format("There was an error assigning task(s). {0} of {1} were not updated.", errorCount, id.Count()), title: "Error", severity: TraceEventType.Error, sticky: true);
			}
			return PartialView("_AssignTaskGroup", taskGroup);
		}

		/// <summary>
		/// Completes the task group.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult CompleteTaskGroup(Guid? id)
		{
			var groupKey = string.Empty;
			if (id == null || id.GetValueOrDefault() == Guid.Empty)
				groupKey = EntityType.Task.ToString();
			else
				groupKey = GetTaskSelectionName(id.Value);
			var hashSetIds = _sessionProvider.GetGroupItems(groupKey);
			var response = CompleteTask(hashSetIds.ToArray(), id);
			return response;
		}

		private JsonResult CompleteTask(Guid[] id, Guid? containerId)
		{
			GrowlMessage message = null;
			bool success = true;
			int errorCount = 0;

			if (id.Length > 0)
			{
				foreach (var guid in id)
				{
					try
					{
						if (containerId.GetValueOrDefault() == Guid.Empty)
						{
							containerId = _searchProvider.GetContainerId(guid, _sessionProvider, _userContext);
						}

						var task = _taskProvider.FetchById(containerId.GetValueOrDefault(), guid, _userContext);
						//BuildTaskDetailModel(task);
						task = _taskProvider.GetTaskDetailModel(task, _userContext);
						if (task == null)
							throw new HttpException((int)HttpStatusCode.NotFound, "Task couldn't be found.");

						if (task.Phase != TaskStatusEnum.Completed || task.Phase != TaskStatusEnum.Canceled)
						{
							if (task.SubTasks.Any())
								UpdateChildTasks(task.SubTasks, id, containerId.Value);

							task.Phase = TaskStatusEnum.Completed;

							var validationErrors = _taskProvider.Validate(guid, task);
							if (validationErrors != null && validationErrors.Any())
								throw new Exception("Task has validation errors.");

							_taskProvider.Update(guid, task);
						}
					}
					catch (Exception exception)
					{
						// If Item is already deleted we are ignoring it. Middle tier is not handling it.
						var ex = exception.GetBaseException() as WebException;
						var isDeletedTask = ex != null && ex.Response as HttpWebResponse != null && (ex.Response as HttpWebResponse).StatusCode == HttpStatusCode.NotFound;

						if (!isDeletedTask)
						{
							errorCount++;
							_logHelper.Log(MessageIds.ProjectControllerTaskDeleteException, LogCategory.Project, LogPriority.High, TraceEventType.Error, ControllerContext.HttpContext, exception.Message, exception);
						}
					}
					finally
					{
						_sessionProvider.RemoveGroupItem(guid, EntityType.Task.ToString());
						_sessionProvider.RemoveGroupItem(guid, GetTaskSelectionName(containerId.Value));
					}
				}
			}

			if (errorCount == 0)
			{
				success = true;
				AddPageMessage("Task(s) have been updated successfully.  It may take a minute before your changes are reflected on the site.", title: "Success!", severity: TraceEventType.Start);
			}
			else
			{
				success = true;
				AddPageMessage(string.Format("There was an error updating task(s). {0} of {1} were not completed.", errorCount, id.Count()), title: "Error", severity: TraceEventType.Error, sticky: true);
			}

			return Json(new
			{
				success,
				message
			});
		}


		internal void UpdateChildTasks(IEnumerable<TaskDetail> childTask, Guid[] ids, Guid containerId)
		{

			foreach (var task in childTask)
			{
				if (task.Phase != TaskStatusEnum.Completed || task.Phase != TaskStatusEnum.Canceled)
				{
					if (task.SubTasks.Any())
						UpdateChildTasks(task.SubTasks, ids, containerId);

					if (ids.Any(x => x.ToString("N") == task.TaskId.Value.ToString("N")))
					{
						task.ContainerId = containerId;
						task.Phase = TaskStatusEnum.Completed;
						var validationErrors = _taskProvider.Validate(task.TaskId.Value, task);
						if (validationErrors != null && validationErrors.Any())
							throw new Exception("Task has validation errors.");

						_taskProvider.Update(task.TaskId.Value, task);
					}

				}
			}
		}


		private void SetSearchMessages(ProjectDetail project)
		{
			// to allow our global search result Display Templates to recognize this page
			ViewBag.ActiveProjectId = project.Id;
			SetNoSearchResultsMessage(EntityType.Project);
		}





		/// <summary>
		/// Returns the session provider group name for project's tasks.
		/// </summary>
		/// <param name="projectContainerId">The project container unique identifier.</param>
		/// <returns></returns>
		private string GetTaskSelectionName(Guid projectContainerId)
		{
			return string.Concat("Task_", projectContainerId.ToString("N"));
		}

		private void SetTaskDetailViewDependencies(TaskDetail taskDetail)
		{
			var isCurrentUserProjectHandler = string.Equals(taskDetail.ProjectHandler, _userContext.LoginId, StringComparison.OrdinalIgnoreCase);

			taskDetail.PercentCompleteList = _portalConfiguration.TaskPercentComplete;
			taskDetail.ProgressList = _portalConfiguration.TaskProgress;
			taskDetail.TaskTypes = _taskTypeProvider.FetchAll()
					.OrderBy(x => x.Name)
					.ToList();
			if (taskDetail.TaskTypeId.HasValue && !isCurrentUserProjectHandler)
			{
				taskDetail.TaskReadonlyFields = GetReadyOnlyFieldsByTaskType(taskDetail.TaskTypeId.Value).ToList();
			}
		}


		private void EnsureContainerId(Guid taskId, TaskDetail taskDetail)
		{
			if (taskDetail.ContainerId == null)
			{
				taskDetail.ContainerId = _searchProvider.GetContainerId(taskId, _sessionProvider, _userContext);
				if (taskDetail.ContainerId.HasValue && ModelState.ContainsKey("ContainerId"))
					ModelState["ContainerId"].Errors.Clear();
			}
		}

		private TaskDetail ValidateTask(Guid id, TaskDetail taskDetail)
		{
			var validationErrors = _taskProvider.Validate(id, taskDetail);
			_taskModelStateValidationStrategy.UpdateModelState(taskDetail, ModelState, validationErrors);
			return taskDetail;
		}

		internal List<TaxonomyMenuItem> TaskHistoryPageActions(Guid taskId, ProjectDetail project)
		{
			var actionsRight = new List<TaxonomyMenuItem>();
			actionsRight.Add(new TaxonomyMenuItem
			{
				Key = "downloadHistory",
				Text = "Export to Excel",
				Url = Url.PageTaskHistoryExportExcel(taskId, project.ContainerId),
				CssClass = "arrow primary"
			});
			return actionsRight;
		}

		/// <summary>
		/// Adds the documents to task.
		/// </summary>
		/// <param name="taskDetail">The task.</param>
		/// <param name="taskDocumentChangeRequest">The document update.</param>
		internal void AddDocumentsToTask(TaskDetail taskDetail, TaskDocumentChangeRequest taskDocumentChangeRequest)
		{
			if (taskDocumentChangeRequest == null)
				return;

			var lastDocumentAddedList = new List<string>();
			foreach (var document in taskDocumentChangeRequest.Current)
			{
				if (taskDocumentChangeRequest.Original.All(x => x.Id != document.Id))
				{
					try
					{
						// ReSharper disable once PossibleInvalidOperationException
						_ariaProvider.LinkDocumentToTask(taskDetail.TaskId.Value, document.Id, document.Title, _userContext);
						lastDocumentAddedList.Add(document.Title);
					}
					catch (Exception exception)
					{
						//errorCount = errorCount + 1;
						_logHelper.Log(MessageIds.ProjectControllerLinkDocumentToTaskException, LogCategory.Project, LogPriority.High, TraceEventType.Error, this.HttpContext, "There was an error encountered attempting to add documents.", exception);
					}
				}
			}
			taskDetail.LastDocumentAdded = String.Join(";", lastDocumentAddedList);

			var lastDocumentRemovedList = new List<string>();
			foreach (var document in taskDocumentChangeRequest.Original)
			{
				if (taskDocumentChangeRequest.Current.All(x => x.Id != document.Id))
				{
					try
					{
						// ReSharper disable once PossibleInvalidOperationException
						_ariaProvider.UnLinkDocumentToTask(taskDetail.TaskId.Value, document.Id, document.Title, _userContext);
						lastDocumentRemovedList.Add(document.Title);
					}
					catch (Exception exception)
					{
						//errorCount = errorCount + 1;
						_logHelper.Log(MessageIds.ProjectControllerRemoveLinkDocumentToTaskException, LogCategory.Project, LogPriority.High, TraceEventType.Error, this.HttpContext, "There was an error encountered attempting to remove documents.", exception);
					}
				}
			}
			taskDetail.LastDocumentRemoved = String.Join(";", lastDocumentRemovedList);
		}

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

		internal List<TaxonomyMenuItem> TaskPageActions(ProjectDetail project, SearchCriteria criteria)
		{
			var id = project.Id;
			var canEditProject = _userContext.CanEditProject(project);
			var actionsRight = new List<TaxonomyMenuItem>();

			actionsRight.Add(new TaxonomyMenuItem
			{
				Key = "downloadTasks",
				Text = "Export to Excel",
				Url = Url.PageTaskExportExcel(id, project.ContainerId, criteria),
				CssClass = "arrow primary"
			});

			if (canEditProject)
			{
				actionsRight.Add(new TaxonomyMenuItem()
				{
					Key = "createTask",
					Text = "Add a Task",
					Url = Url.PageCreateTask(id, project.ContainerId),
					Modal = true,
					LinkData = { { "class", "add" } }
				});
			}

			actionsRight.Add(new TaxonomyMenuItem()
			{
				Key = "addToFavorites",
				Text = "Add To Favorites",
				Url = "#",
				Modal = true,
				LinkData = { { "class", "add" }, { "DataUrl", Url.PageCreateFavorite() } }
			});

			return actionsRight;
		}

		private IEnumerable<TaxonomyMenuItem> TaskDetailActions(TaskDetail task, Guid projectId, Guid containerId, TaskType taskType)
		{
			var actionsRight = new List<TaxonomyMenuItem>();
			var isClosed = task.Phase == TaskStatusEnum.Completed || task.Phase == TaskStatusEnum.Canceled;

			if (!isClosed)
			{
				actionsRight.Add(new TaxonomyMenuItem
				{
					Key = "editTask",
					Text = "Edit Task",
					Url = Url.PageEditTask(task.TaskId.GetValueOrDefault(), containerId),
					Modal = true,
					CssClass = "arrow primary"
				});
			}

			actionsRight.Add(new TaxonomyMenuItem()
			{
				Key = "addATask",
				Text = "Add a Task",
				Url = Url.PageCreateTask(projectId, containerId),
				Modal = true,
				CssClass = "arrow"
			});

			if (!isClosed)
			{
				actionsRight.Add(new TaxonomyMenuItem
				{
					Key = "associateDocuments",
					Text = "Associate Documents",
					Url = Url.PageEditTask(task.TaskId.GetValueOrDefault(), containerId) + "&tab=modal-task-documents",
					Modal = true,
					CssClass = "arrow"
				});
			}

			if (taskType == null)
			{
				return actionsRight;
			}

			actionsRight.AddRange(taskType.Links.Select(link => new TaxonomyMenuItem
			{
				Key = link.Id.ToString(),
				Text = link.Label,
				Url = link.RootUrl.Replace("{containerid}", containerId.ToString()).Replace("{taskid}", task.TaskId.ToString()),
				Modal = link.IsModal,
				CssClass = "arrow",
				NewWindow = !(link.IsModal)
			}));

			return actionsRight;
		}

		private IEnumerable<TaxonomyMenuItem> TaskNavigationLinks(Guid id, IProjectSection project)
		{
			var leftNav = new List<TaxonomyMenuItem>();

			leftNav.Add(new TaxonomyMenuItem()
			{
				Key = EntityType.Task.ToString(),
				Text = "Task Details",
				IsRefinable = false,
				Url = Url.PageViewTask(id, project.ContainerId)
			});

			leftNav.Add(new TaxonomyMenuItem()
			{
				Key = EntityType.Task.ToString(),
				Text = "Task History",
				IsRefinable = false,
				Url = Url.PageTaskHistory(id, project.Id)
			});

			return leftNav;
		}

		internal IEnumerable<Breadcrumb> TaskBreadcrumbs(ProjectDetail project, TaskDetail task, string crumbText)
		{
			var name = project.Name;
			var pageName = EntityType.Project.GetDisplayName();
			var taskName = EntityType.Task.GetDisplayName();
			var crumbs = new List<Breadcrumb> { new Breadcrumb { Text = pageName.Pluralize(), Url = Url.PageSearchProjects() } };
			crumbs.Add(new Breadcrumb { Text = name, Url = Url.PageProjectOverview(project.Id) });
			crumbs.Add(new Breadcrumb() { Text = taskName.Pluralize(), Url = Url.PageProjectTasks(project.Id) });
			crumbs.Add(new Breadcrumb { Text = task.TaskName, Url = Url.PageViewTask(task.TaskId.GetValueOrDefault(), project.ContainerId) });

			if (crumbText != null)
			{
				crumbs.Add(new Breadcrumb { Text = crumbText, Url = Url.Action(null) });
				ViewBag.SearchTitle = crumbText.Pluralize();
			}

			SetPageMetadata(pageName, crumbs, trackingTitles: new Pair<string, string>(name, project.Id.ToString("N")));

			return ViewBag.BreadCrumbs;
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

		internal List<TaxonomyMenuItem> ProjectActions(ProjectDetail project, IUserContext userContext, UrlHelper url)
		{
			return _projectActionManager.GetProjectActions(project, userContext, url);
		}

	}
}