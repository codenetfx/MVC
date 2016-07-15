using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Providers;
using UL.Aria.Web.Common.Models.UserTeam;
using UL.Enterprise.Foundation.Logging;
using System.Diagnostics;
using UL.Aria.Web.Common.Mvc;

namespace UL.Aria.Web.Portal.Controllers
{
    /// <summary>
    /// User Team controller.
    /// </summary>
    public class UserTeamController : BaseController
    {
        private readonly IUserTeamProvider _userTeamProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="UserTeamController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="userTeamProvider">The user team provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
		public UserTeamController(IUserContext userContext, ILogHelper logHelper, IPortalConfiguration portalConfiguration, IUserTeamProvider userTeamProvider,
            ITempDataProviderFactory tempDataProviderFactory)
			: base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
		{
		    _userTeamProvider = userTeamProvider;
		}

        /// <summary>
        /// Edits this instance.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult Edit()
        {
            var userTeamInfo = _userTeamProvider.FetchByUserId(_userContext.UserId);

            return PartialView("_Edit", userTeamInfo);
        }

        /// <summary>
        /// Edits the specified user team information.
        /// </summary>
        /// <param name="userTeamInfo">The user team information.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Edit(UserTeamInfo userTeamInfo)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    _userTeamProvider.UpdateTeamMembership(userTeamInfo.TeamMembers, userTeamInfo.Id.GetValueOrDefault());
                    userTeamInfo = _userTeamProvider.FetchByUserId(_userContext.UserId);
                    ViewBag.Success = true;
                    var message = string.Format("<strong> {0} </strong> has been successfully saved.", userTeamInfo.Name);
                    AddPageMessage(message, severity: TraceEventType.Information, title: "Success!");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.GetBaseException().Message);
                _logHelper.Log(Logging.MessageIds.UserTeamException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext, "Error while updating team", ex);
            }

            return PartialView("_Edit", userTeamInfo);
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
    }
}
