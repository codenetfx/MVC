using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using UL.Aria.Common.Authorization;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Project;
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
    ///     Provides a Controller for Links
    /// </summary>
    [AuthorizeClaim(Resource = SecuredResources.ProjectTemplateAdministration, Action = SecuredActions.View)]
    public class LinkController : TemplateAdminBaseController
    {
        private readonly ILinkProvider _linkProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="LinkController" /> class.
        /// </summary>
        /// <param name="linkProvider">The link provider.</param>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
        public LinkController(ILinkProvider linkProvider, IUserContext userContext, ILogHelper logHelper,
            IPortalConfiguration portalConfiguration, ITempDataProviderFactory tempDataProviderFactory)
            : base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
        {
            _linkProvider = linkProvider;
        }

        /// <summary>
        ///     Indexes the specified search criteria.
        /// </summary>
        /// <param name="searchCriteria">The search criteria.</param>
        /// <returns></returns>
        public ActionResult Index(SearchCriteria searchCriteria)
        {
            if (searchCriteria == null)
            {
                searchCriteria = new SearchCriteria();
            }

            searchCriteria = searchCriteria.ApplyLinkSearch();

            var model = _linkProvider.Search(searchCriteria, _userContext);



            //// TODO - Stubbed out until service returns these values.
            //model.Results.ForEach(sr =>
            //{
            //    sr.CreatedByLoginId = "change@me.com";
            //    sr.UpdatedByLoginId = "change@me.com";
            //    sr.BusinessUnits = new List<BusinessUnit>
            //    {
            //        new BusinessUnit {Code = "CHANGE_ME"}
            //    };
            //});



            model.PageLinks = ActionsLeft(EntityType.Link.ToString());
            model.Breadcrumbs = Breadcrumbs("Link", true);
            model.PageActions = ActionsRight(EntityType.Link);

            return View("Search", model);
        }

        /// <summary>
        ///     Creates this instance.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Create()
        {
            return PartialView("_Create", _linkProvider.InitializeEditable());
        }

        /// <summary>
        ///     Creates the specified Link.
        /// </summary>
        /// <param name="link">The Link.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Create(Link link)
        {
            try
            {
                ValidateLink(link);
                if (ModelState.IsValid)
                {
                    _linkProvider.Create(link, _userContext);
                    ViewBag.Success = true;
                    var message = string.Format("Link <strong> {0} </strong> has been successfully created.",
                        link.DisplayName);
                    AddPageMessage(message, severity: TraceEventType.Information, title: "Success!");
                }
            }
            catch (Exception exception)
            {
                ModelState.AddModelError("", exception.GetBaseException().Message);
                _logHelper.Log(MessageIds.LinkException, LoggingCategory, LogPriority.High, TraceEventType.Error,
                    HttpContext, "Error while creating Link", exception);
            }
            return PartialView("_Create", link);
        }

        /// <summary>
        ///     Edits the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult Edit(Guid id)
        {
            var link = _linkProvider.FetchByIdForEdit(id);

            return PartialView("_Edit", link);
        }

        /// <summary>
        ///     Edits the specified Link.
        /// </summary>
        /// <param name="link">The Link.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Edit(Link link)
        {
            try
            {
                ValidateLink(link);
                if (ModelState.IsValid)
                {
                    _linkProvider.Update(link, _userContext);
                    ViewBag.Success = true;
                    var message = string.Format("Link <strong> {0} </strong> has been successfully updated.",
                        link.DisplayName);
                    AddPageMessage(message, severity: TraceEventType.Information, title: "Success!");
                }
            }
            catch (Exception exception)
            {
                ModelState.AddModelError("", exception.GetBaseException().Message);
                _logHelper.Log(MessageIds.LinkException, LoggingCategory, LogPriority.High, TraceEventType.Error,
                    HttpContext, "Error while updating Link", exception);
            }
            return PartialView("_Edit", link);
        }

        /// <summary>
        /// Deactivates the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public JsonResult Deactivate(Guid id)
        {
            GrowlMessage message = null;
            var success = false;
            try
            {
                _linkProvider.Delete(id, _userContext);
                success = true;
                message = CreatePageMessage("Link has been deactivated successfully.", title: "Success!",
                    severity: TraceEventType.Information);
                AddPageMessage(message);
            }
            catch (Exception exception)
            {
                var ex = exception.GetBaseException() as WebException;

                message =
                    CreatePageMessage(
                        "There was an error deactivating Link. " + exception.GetBaseException().Message,
                        title: "Error", severity: TraceEventType.Error, sticky: true);

                _logHelper.Log(MessageIds.LinkException, LoggingCategory, LogPriority.High, TraceEventType.Error,
                    HttpContext, "Error while Deactivating Link", exception);

                AddPageMessage(message);
            }

            return Json(new
            {
                success,
                message
            });
        }

        internal void ValidateLink(Link link)
        {
            if (!link.BusinessUnits.Any(x => x.Selected))
            {
                ModelState.AddModelError("BusinessUnits", "Business Unit is Required");
            }
        }
    }
}