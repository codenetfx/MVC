using System;
using System.Diagnostics;
using System.Net;
using System.Web.Mvc;
using UL.Aria.Common.Authorization;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Search;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Models.TaskCategory;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Mvc.Attributes;
using UL.Aria.Web.Common.Providers;
using UL.Enterprise.Foundation.Logging;

namespace UL.Aria.Web.Portal.Controllers
{
	/// <summary>
	/// Provides a Controller for Task Templates
	/// </summary>
	[AuthorizeClaim(Resource = SecuredResources.ProjectTemplateAdministration, Action = SecuredActions.View)]
	public class TaskCategoryController : TemplateAdminBaseController
	{
		private readonly ITaskCategoryProvider _taskCategoryProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskCategoryController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="taskCategoryProvider">The task template provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
		public TaskCategoryController(IUserContext userContext, ILogHelper logHelper, IPortalConfiguration portalConfiguration,
            ITaskCategoryProvider taskCategoryProvider, ITempDataProviderFactory tempDataProviderFactory)
			: base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
		{
			_taskCategoryProvider = taskCategoryProvider;
		}

		/// <summary>
		/// Indexes the specified search criteria.
		/// </summary>
		/// <param name="searchCriteria">The search criteria.</param>
		/// <returns></returns>
		public ActionResult Index(SearchCriteria searchCriteria)
		{
			if (searchCriteria == null)
			{
				searchCriteria = new SearchCriteria();
			}

			searchCriteria = searchCriteria.ApplyTaskTemplateSearch();

			var model = _taskCategoryProvider.Search(searchCriteria, _userContext);

			model.PageLinks = ActionsLeft(EntityType.TaskCategory.ToString());
			model.Breadcrumbs = Breadcrumbs("Task Categories", true);
			model.PageActions = ActionsRight(EntityType.TaskCategory);

			return View("Search", model);
		}


		/// <summary>
		/// Creates this instance.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		public ActionResult Create()
		{
			var model = new TaskCategoryCreate();
			return PartialView("_Create", model);
		}

		/// <summary>
		/// Creates the specified task template.
		/// </summary>
		/// <param name="taskCategory">The task template.</param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult Create(TaskCategoryCreate taskCategory)
		{
			try
			{
				if (ModelState.IsValid)
				{
					_taskCategoryProvider.Create(taskCategory, _userContext);
					ViewBag.Success = true;
					var message = string.Format("Task Category <strong> {0} </strong> has been successfully created.", taskCategory.Name);
					AddPageMessage(message, severity: TraceEventType.Information, title: "Success!");
				}
			}
			catch (Exception exception)
			{

				ModelState.AddModelError("", exception.GetBaseException().Message);
				_logHelper.Log(Logging.MessageIds.TaskCategoryException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext, "Error while creating task category", exception);
			}
			return PartialView("_Create", taskCategory);
		}

		/// <summary>
		/// Edits the specified identifier.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <returns></returns>
		public ActionResult Edit(Guid id)
		{
			var taskinfo = _taskCategoryProvider.FetchById(id, _userContext);
			var model = new TaskCategoryCreate() { Id = taskinfo.Id, Name = taskinfo.Name, Description = taskinfo.Description };

			return PartialView("_Edit", model);
		}

		/// <summary>
		/// Edits the specified task template.
		/// </summary>
		/// <param name="taskCategory">The task template.</param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult Edit(TaskCategoryCreate taskCategory)
		{
			try
			{
				if (ModelState.IsValid)
				{
					_taskCategoryProvider.Update(taskCategory, _userContext);
					ViewBag.Success = true;
					var message = string.Format("Task Category <strong> {0} </strong> has been successfully updated.", taskCategory.Name);
					AddPageMessage(message, severity: TraceEventType.Information, title: "Success!");
				}
			}
			catch (Exception exception)
			{
				ModelState.AddModelError("", exception.GetBaseException().Message);
				_logHelper.Log(Logging.MessageIds.TaskCategoryException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext, "Error while updating task category", exception);
			}
			return PartialView("_Edit", taskCategory);
		}


		/// <summary>
		/// Deletes the specified identifier.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <returns></returns>
		public JsonResult Delete(Guid id)
		{
			GrowlMessage message = null;
			bool success = false;
			try
			{
				_taskCategoryProvider.Delete(id);
				success = true;
				message = CreatePageMessage("Task Category have been deleted successfully.", title: "Success!",
					severity: TraceEventType.Information);
				AddPageMessage(message);

			}
			catch (Exception exception)
			{

				var ex = exception.GetBaseException() as WebException;
				var canDeleteTaskCategory = ex != null && (ex.Response as HttpWebResponse) != null && (ex.Response as HttpWebResponse).StatusCode == HttpStatusCode.MethodNotAllowed;

				if (canDeleteTaskCategory)
				{
					message = CreatePageMessage("Task Category is in use, can't delete it.", title: "Error", severity: TraceEventType.Error, sticky: true);
				}
				else
				{
					message = CreatePageMessage("There was an error deleting Task Category." + exception.GetBaseException().Message, title: "Error", severity: TraceEventType.Error, sticky: true);
				}
				_logHelper.Log(Logging.MessageIds.TaskCategoryException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext, "Error while Deleting task category", exception);
				
				AddPageMessage(message);
			}

			return Json(new
			{
				success,
				message
			});
		}

	}
}
