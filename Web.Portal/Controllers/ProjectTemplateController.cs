using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UL.Aria.Common.Authorization;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Project;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Mvc.Attributes;
using UL.Aria.Web.Common.Providers;
using UL.Enterprise.Foundation.Logging;

namespace UL.Aria.Web.Portal.Controllers
{
    /// <summary>
    /// Project Template Controller
    /// </summary>
    [AuthorizeClaim(Resource = SecuredResources.ProjectTemplateAdministration, Action = SecuredActions.View)]
    public class ProjectTemplateController : BaseController
    {
        private readonly IProjectTemplateProvider _projectTemplateProvider;
        private readonly IBusinessUnitProvider _businessUnitProvider;
        private readonly ITaskTypeProvider _taskTypeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProjectTemplateController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="projectTemplateProvider">The project template provider.</param>
        /// <param name="businessUnitProvider">The business unit provider.</param>
        /// <param name="taskTypeProvider">The task type provider.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
        public ProjectTemplateController(IUserContext userContext, IProjectTemplateProvider projectTemplateProvider, IBusinessUnitProvider businessUnitProvider,
            ITaskTypeProvider taskTypeProvider, IPortalConfiguration portalConfiguration, ILogHelper logHelper, ITempDataProviderFactory tempDataProviderFactory)
            : base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
        {
            _projectTemplateProvider = projectTemplateProvider;
            _businessUnitProvider = businessUnitProvider;
            _taskTypeProvider = taskTypeProvider;
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
        /// Creates this instance.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Create()
        {
            var model = new ProjectTemplateCreate()
            {
                TaskTypes = _taskTypeProvider.FetchAll().OrderBy(x=>x.Name).ToList(),
                BusinessUnits = _businessUnitProvider.FetchAll().ConvertAll(x => new SelectListItem(){ Text = x.Code, Value = x.Id.Value.ToString("N") })
            };

            return PartialView("_Create", model);
        }

        /// <summary>
        /// Creates the specified project template.
        /// </summary>
        /// <param name="projectTemplate">The project template.</param>
        /// <param name="saveDraft">The save draft.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Create(ProjectTemplateCreate projectTemplate, string saveDraft)
        {
            try
            {
                if (saveDraft != projectTemplate.Publish)
                    projectTemplate.IsDraft = true;
                else
                {
	                projectTemplate.Version = 1;
                }

                ValidateProjectTemplate(projectTemplate);
                if (ModelState.IsValid)
                {
                    _projectTemplateProvider.Create(projectTemplate, _userContext);
                    ViewBag.Success = true;

                    var draft = saveDraft != projectTemplate.Publish ? "Saved as Draft" : "Published";
                    var message = string.Format("Project Template <strong> {0} </strong> has been successfully {1}.", projectTemplate.Name,draft);
                    AddPageMessage(message, severity: TraceEventType.Information, title: "Success!");
                }
                else
                {
					projectTemplate.TaskTypes = _taskTypeProvider.FetchAll().OrderBy(x => x.Name).ToList();
                    //projectTemplate.AllTaskTypes = _taskTypeProvider.GetLookups(true).ToList();
                    projectTemplate.NewTaskNumbers = projectTemplate.Tasks.Select(t => t.TaskNumber).ToList();
                }
            }
            catch (Exception exception)
            {
                ModelState.AddModelError("", exception.GetBaseException().Message);
                _logHelper.Log(Logging.MessageIds.ProjectTemplateCreateException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext, "Error while creating project template", exception);
				projectTemplate.TaskTypes = _taskTypeProvider.FetchAll().OrderBy(x => x.Name).ToList();
                //projectTemplate.AllTaskTypes = _taskTypeProvider.GetLookups(true).ToList();
                projectTemplate.NewTaskNumbers = projectTemplate.Tasks.Select(t => t.TaskNumber).ToList();
            }
            return PartialView("_Create", projectTemplate);
        }


        /// <summary>
        /// Edits the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult Edit(Guid id)
        {
            var model = _projectTemplateProvider.FetchEditableById(id);
            if (model == null)
                throw new HttpException(404, "Project Template Could not find");
           
            //MVC Routing doesn't allow id to reset. Re-routing the same action with new id
            if(model.Id.GetValueOrDefault() !=id)
                return RedirectToAction("Edit", new { id = model.Id.Value });
            
            return PartialView("_Edit", model);
        }

        
        /// <summary>
        /// Edits the specified project template.
        /// </summary>
        /// <param name="projectTemplate">The project template.</param>
        /// <param name="saveDraft">The save draft.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Edit(ProjectTemplateCreate projectTemplate, string saveDraft)
        {

            try
            {
                if (saveDraft != projectTemplate.Publish)
                    projectTemplate.IsDraft = true;

                ValidateProjectTemplate(projectTemplate);
                
                if (ModelState.IsValid)
                {
                    _projectTemplateProvider.Update(projectTemplate, _userContext);
                    ViewBag.Success = true;
                    var draft = saveDraft != projectTemplate.Publish ? "Saved as Draft" : "Published";
                    var message = string.Format("Project Template <strong> {0} </strong> has been successfully {1}.", projectTemplate.Name, draft);
                    AddPageMessage(message, severity: TraceEventType.Information, title: "Success!");
                }
                else
                {
					projectTemplate.TaskTypes = _taskTypeProvider.FetchAll().OrderBy(x => x.Name).ToList();
					//projectTemplate.AllTaskTypes = _taskTypeProvider.FetchAll().OrderBy(x => x.Name).ToList();
                    projectTemplate.NewTaskNumbers = _projectTemplateProvider.DetermineNewTaskNumbers(projectTemplate);
                }
            }
            catch (Exception exception)
            {
                ModelState.AddModelError("", exception.GetBaseException().Message);
                _logHelper.Log(Logging.MessageIds.ProjectTemplateCreateException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext, "Error while updating project template", exception);
				projectTemplate.TaskTypes = _taskTypeProvider.FetchAll().OrderBy(x => x.Name).ToList();
				//projectTemplate.AllTaskTypes = _taskTypeProvider.FetchAll().OrderBy(x => x.Name).ToList();
                projectTemplate.NewTaskNumbers = _projectTemplateProvider.DetermineNewTaskNumbers(projectTemplate);
            }

            return PartialView("_Edit", projectTemplate);
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
                _projectTemplateProvider.Delete(id);
                success = true;
                message = CreatePageMessage("Project Template have been deleted successfully.", title: "Success!",
                    severity: TraceEventType.Information);
                AddPageMessage(message);

            }
            catch (Exception exception)
            {
                _logHelper.Log(Logging.MessageIds.ProjectTemplateCreateException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext, "Error while Deleting project template", exception);
                message = CreatePageMessage("There was an error deleting template.", title: "Error", severity: TraceEventType.Error, sticky: true);
                AddPageMessage(message);
            }

            return Json(new
            {
                success,
                message
            });
        }

        /// <summary>
        /// Details the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult Details(Guid id)
        {
            return View();
        }

        private void ValidateProjectTemplate(ProjectTemplateCreate projectTemplate)
        {

           
            if (!projectTemplate.BusinessUnits.Any(x => x.Selected))
            {
                ModelState.AddModelError("BusinessUnits", "Business Unit is Required");
            }

            if (projectTemplate.Tasks != null)
            {
                var taskIds = projectTemplate.Tasks.Select(x => x.TaskNumber);

                foreach (var task in projectTemplate.Tasks.Where(x => x.TaskNumber == x.ParentTaskNumber))
                {
                    ModelState.AddModelError(string.Format("Tasks[{0}].ParentTaskNumber", task.TaskNumber),
                        "Parent Task Number should not equal to TaskNumber.");
                }

                foreach (
                    var task in
                        projectTemplate.Tasks.Where(
                            x => x.ParentTaskNumber.HasValue &&  !taskIds.Contains( x.ParentTaskNumber.Value)))
                {
                    ModelState.AddModelError(string.Format("Tasks[{0}].ParentTaskNumber", task.TaskNumber),
                        "Parent Task Number should exists.");
                }

                foreach (
                    var task in
                        projectTemplate.Tasks.Where(x =>  x.Predecessors.Any(r => !taskIds.Contains(r.TaskNumber))))
                {
                    ModelState.AddModelError(string.Format("Tasks[{0}].PredecessorTask", task.TaskNumber),
                        "Predecessor Task Number should exists.");
                }
            }

            if (ModelState.IsValid == false)
            {
				projectTemplate.TaskTypes = _taskTypeProvider.FetchAll().OrderBy(x => x.Name).ToList();
                //projectTemplate.AllTaskTypes = _taskTypeProvider.GetLookups(true).OrderBy(x => x.Text).ToList();
            }
        }
    }
}
