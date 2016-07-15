using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Web.Mvc;
using UL.Aria.Common.Authorization;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Project;
using UL.Aria.Web.Common.Models.Search;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Mvc.Attributes;
using UL.Aria.Web.Common.Providers;
using UL.Aria.Web.Portal.Logging;
using UL.Enterprise.Foundation.Logging;

namespace UL.Aria.Web.Portal.Controllers
{
    /// <summary>
    ///     Provides a Controller for businessUnits
    /// </summary>
    [AuthorizeClaim(Resource = SecuredResources.AriaAdministration, Action = SecuredActions.View)]
    public class BusinessUnitController : TemplateAdminBaseController
    {
        private readonly IBusinessUnitProvider _businessUnitProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="BusinessUnitController" /> class.
        /// </summary>
        /// <param name="businessUnitProvider">The businessUnit provider provider.</param>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
        public BusinessUnitController(IBusinessUnitProvider businessUnitProvider, IUserContext userContext,
            ILogHelper logHelper, IPortalConfiguration portalConfiguration, ITempDataProviderFactory tempDataProviderFactory)
            : base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
        {
            _businessUnitProvider = businessUnitProvider;
        }

        /// <summary>
        ///     Indexes the specified search criteria.
        /// </summary>
        /// <param name="searchCriteria">The search criteria.</param>
        /// <returns></returns>
        public ActionResult Index(SearchCriteria searchCriteria)
        {
            if (searchCriteria == null)
                searchCriteria = new SearchCriteria();

            searchCriteria.ApplyBusinessUnitSearch();
            var model = _businessUnitProvider.Search(searchCriteria, _userContext);
            model.PageLinks = ActionsLeft(EntityType.BusinessUnit.ToString());
            model.Breadcrumbs = Breadcrumbs("Business Units", true);
            model.PageActions = ActionsRight(EntityType.BusinessUnit);
            return View("Search", model);
        }

        /// <summary>
        ///     Creates this instance.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Create()
        {
            var model = new BusinessUnit();
            return PartialView("_Create", model);
        }

        /// <summary>
        ///     Creates the specified Business Unit.
        /// </summary>
        /// <param name="businessUnit">The Business Unit.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Create(BusinessUnit businessUnit)
        {
            try
            {
                Validate(businessUnit);
                if (ModelState.IsValid)
                {
                    _businessUnitProvider.Create(businessUnit, _userContext);
                    ViewBag.Success = true;
                    var message = string.Format("Business Unit <strong> {0} </strong> has been successfully created.",
                        businessUnit.Name);
                    AddPageMessage(message, severity: TraceEventType.Information, title: "Success!");
                }
            }
            catch (Exception exception)
            {
                ModelState.AddModelError("", exception.GetBaseException().Message);
                _logHelper.Log(MessageIds.BusinessUnitException, LoggingCategory, LogPriority.High, TraceEventType.Error,
                    HttpContext, "Error while creating Business Unit", exception);
            }
            return PartialView("_Create", businessUnit);
        }

        private void Validate(BusinessUnit businessUnit)
        {
            var validationErrors = _businessUnitProvider.Validate(businessUnit, _userContext);
            if (null != validationErrors)
            {
                foreach (var validationError in validationErrors)
                {
                    ModelState.AddModelError("", validationError);
                    _logHelper.Log(MessageIds.BusinessUnitException, LoggingCategory, LogPriority.High, TraceEventType.Error,
                        HttpContext, validationError);
                }
            }
        }

        /// <summary>
        ///     Edits the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult Edit(Guid id)
        {
            var businessUnit = _businessUnitProvider.FetchById(id, _userContext);

            return PartialView("_Edit", businessUnit);
        }

        /// <summary>
        ///     Edits the specified Business Unit.
        /// </summary>
        /// <param name="businessUnit">The Business Unit.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Edit(BusinessUnit businessUnit)
        {
            try
            {
                Validate(businessUnit);
                if (ModelState.IsValid)
                {
                    _businessUnitProvider.Update(businessUnit, _userContext);
                    ViewBag.Success = true;
                    var message = string.Format("Business Unit <strong> {0} </strong> has been successfully updated.",
                        businessUnit.Name);
                    AddPageMessage(message, severity: TraceEventType.Information, title: "Success!");
                }
            }
            catch (Exception exception)
            {
                ModelState.AddModelError("", exception.GetBaseException().Message);
                _logHelper.Log(MessageIds.BusinessUnitException, LoggingCategory, LogPriority.High, TraceEventType.Error,
                    HttpContext, "Error while updating Business Unit", exception);
            }
            return PartialView("_Edit", businessUnit);
        }

        /// <summary>
        ///     Deletes the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public JsonResult Delete(Guid id)
        {
            GrowlMessage message = null;
            var success = false;
            try
            {
                _businessUnitProvider.Delete(id, _userContext);
                success = true;
                message = CreatePageMessage("Business Unit have been deleted successfully.", title: "Success!",
                    severity: TraceEventType.Information);
                AddPageMessage(message);
            }
            catch (Exception exception)
            {
                var ex = exception.GetBaseException() as WebException;

                message =
                    CreatePageMessage(
                        "There was an error deleting Business Unit." + exception.GetBaseException().Message,
                        title: "Error", severity: TraceEventType.Error, sticky: true);

                _logHelper.Log(MessageIds.BusinessUnitException, LoggingCategory, LogPriority.High, TraceEventType.Error,
                    HttpContext, "Error while Deleting Business Unit", exception);

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