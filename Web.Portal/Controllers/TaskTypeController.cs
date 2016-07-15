using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using UL.Aria.Common.Authorization;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Search;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Models.TaskType;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Mvc.Attributes;
using UL.Aria.Web.Common.Providers;
using UL.Aria.Web.Portal.Logging;
using UL.Enterprise.Foundation.Logging;

namespace UL.Aria.Web.Portal.Controllers
{
    /// <summary>
    /// Provides a Controller for Task Types
    /// </summary>
    [AuthorizeClaim(Resource = SecuredResources.ProjectTemplateAdministration, Action = SecuredActions.View)]
    public class TaskTypeController : TemplateAdminBaseController
    {
        private readonly ITaskTypeProvider _taskTypeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TaskTypeController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="taskTypeProvider">The task type provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
        public TaskTypeController(IUserContext userContext, ILogHelper logHelper, IPortalConfiguration portalConfiguration, ITaskTypeProvider taskTypeProvider,
            ITempDataProviderFactory tempDataProviderFactory)
            : base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
		{
			_taskTypeProvider = taskTypeProvider;
		}
        
        /// <summary>
        /// Indexes this instance.
        /// </summary>
        /// <returns></returns>
		public ActionResult Index(SearchCriteria searchCriteria)
        {
			if (searchCriteria == null)
				searchCriteria = new SearchCriteria();

			searchCriteria.ApplyTaskTypeSearch();
			var model = _taskTypeProvider.Search(searchCriteria, _userContext);
			model.PageLinks = ActionsLeft(EntityType.TaskType.ToString());
			model.Breadcrumbs = Breadcrumbs("Predefined Tasks", true);
			model.PageActions = ActionsRight(EntityType.TaskType);
			return View("~/Views/TemplateAdmin/Search.cshtml", model);
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
				_taskTypeProvider.Delete(id);
				success = true;
				message = CreatePageMessage("Predefined Task has been deactivated successfully.", title: "Success!",
					severity: TraceEventType.Information);
				AddPageMessage(message);

			}
			catch (Exception exception)
			{

				var ex = exception.GetBaseException() as WebException;
				var isTaskTypeInUse = ex != null && (ex.Response as HttpWebResponse) != null && (ex.Response as HttpWebResponse).StatusCode == HttpStatusCode.MethodNotAllowed;

				if (isTaskTypeInUse)
				{
                    message = CreatePageMessage("Predefined Task is in use, can't deactivate it.", title: "Error", severity: TraceEventType.Error, sticky: true);
				}
				else
				{
                    message = CreatePageMessage("There was an error deactivating Predefined Task.", title: "Error", severity: TraceEventType.Error, sticky: true);
				}
                _logHelper.Log(Logging.MessageIds.ProjectTemplateCreateException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext, "Error while Deactivating Predefined Task", exception);
				
				AddPageMessage(message);
			}

			return Json(new
			{
				success,
				message
			});
		}

        /// <summary>
        /// Edits the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Edit(Guid id)
        {
            var model = _taskTypeProvider.FetchEditableById(id);
			if (model == null)
			{
                throw new HttpException(404, "Predefined Task could not be found.");
			}
			
            return PartialView("_Edit", model);
        }

        /// <summary>
        /// Edits the specified task type information.
        /// </summary>
        /// <param name="taskTypeEditable">The task type information.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Edit(TaskTypeEditable taskTypeEditable)
        {
            try
            {
                ValidateTaskType(taskTypeEditable);

                if (ModelState.IsValid)
                {
                    _taskTypeProvider.Update(taskTypeEditable);
                    ViewBag.Success = true;
                    var message = string.Format("Predefined Task <strong> {0} </strong> has been successfully saved.", taskTypeEditable.Name);
                    AddPageMessage(message, severity: TraceEventType.Information, title: "Success!");
                }
                else
                {
                    taskTypeEditable = _taskTypeProvider.InitializeEditable(taskTypeEditable);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.GetBaseException().Message);
                _logHelper.Log(Logging.MessageIds.ProjectTemplateCreateException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext, "Error while updating predefined task", ex);
            }

            return PartialView("_Edit", taskTypeEditable);
        }

        /// <summary>
        /// Creates the specified identifier.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Create()
        {
            return PartialView("_Create", _taskTypeProvider.InitializeEditable());
        }

        /// <summary>
        /// Creates the specified task type information.
        /// </summary>
        /// <param name="taskTypeEditable">The task type information.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Create(TaskTypeEditable taskTypeEditable)
        {
            try
            {
                ValidateTaskType(taskTypeEditable);

                if (ModelState.IsValid)
                {
                    _taskTypeProvider.Create(taskTypeEditable);
                    ViewBag.Success = true;
                    var message = string.Format("Predefined Task <strong> {0} </strong> has been successfully saved.", taskTypeEditable.Name);
                    AddPageMessage(message, severity: TraceEventType.Information, title: "Success!");
                }
                else
                {
                    taskTypeEditable = _taskTypeProvider.InitializeEditable(taskTypeEditable);
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.GetBaseException().Message);
                _logHelper.Log(Logging.MessageIds.ProjectTemplateCreateException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext, "Error while updating predefined task", ex);
            }

            return PartialView("_Create", taskTypeEditable);
        }

        internal void ValidateTaskType(TaskTypeEditable taskType)
        {
            if (!taskType.BusinessUnits.Any(x => x.Selected))
            {
                ModelState.AddModelError("BusinessUnits", "Business Unit selection is required.");
            }

            if ((taskType.BusinessUnits.Any(x => x.Text.ToLower() == "all" && x.Selected))
                && (taskType.BusinessUnits.Count(x => x.Selected) > 1))
            {
                ModelState.AddModelError("BusinessUnits", "If Business Unit 'ALL' is selected, no other Business Units can be selected.");
            }

            if (!ModelState.IsValid)
            {
                return;
            }

            var validationErrors = _taskTypeProvider.Validate(taskType);
            if (null != validationErrors)
            {
                foreach (var validationError in validationErrors)
                {
                    ModelState.AddModelError("", validationError);
                    _logHelper.Log(MessageIds.TaskTypeCreateException, LoggingCategory, LogPriority.High, TraceEventType.Error,
                        HttpContext, validationError);
                }
            }
        }
    }
}
