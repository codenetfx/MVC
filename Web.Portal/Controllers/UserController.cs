using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.Practices.Unity.Utility;
using UL.Aria.Common;
using UL.Aria.Common.Authorization;
using UL.Aria.Web.Portal.Logging;
using UL.Enterprise.Foundation;
using UL.Enterprise.Foundation.Logging;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Claims;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Company;
using UL.Aria.Web.Common.Models.Container;
using UL.Aria.Web.Common.Models.Profile;
using UL.Aria.Web.Common.Models.Search;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Mvc.Attributes;
using UL.Aria.Web.Common.Providers;

namespace UL.Aria.Web.Portal.Controllers
{
    /// <summary>
    /// Pages focused on users and their assets
    /// </summary>
    [CompanyAuthorizeClaim(Resource = SecuredResources.CompanyUserAdministration, Action = SecuredActions.View)]
    public class UserController : BaseController
    {
        private readonly IProfileProvider _profileProvider;
        private readonly ISearchProvider _searchProvider;
        private readonly IUserBusinessClaimProvider _userBusinessClaimProvider;
        private readonly IUserBusinessClaimFactory _claimFactory;
	    private readonly IEmailProvider _emailProvider;


        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyController" /> class.
        /// </summary>
        /// <param name="profileProvider">The profile provider.</param>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="searchProvider">The search provider.</param>
        /// <param name="userBusinessClaimProvider">The user business claim provider.</param>
        /// <param name="claimFactory">The claim factory.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="emailProvider">The email provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
	    public UserController(IProfileProvider profileProvider, IUserContext userContext, ILogHelper logHelper, ISearchProvider searchProvider,
            IUserBusinessClaimProvider userBusinessClaimProvider, IUserBusinessClaimFactory claimFactory, IPortalConfiguration portalConfiguration,
            IEmailProvider emailProvider, ITempDataProviderFactory tempDataProviderFactory)
            : base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
        {
            _profileProvider = profileProvider;
            _searchProvider = searchProvider;
            _userBusinessClaimProvider = userBusinessClaimProvider;
            _claimFactory = claimFactory;
	        _emailProvider = emailProvider;
        }

        private UserProfile GetUserOr404(Guid id)
        {
            UserProfile user = _profileProvider.FetchById(id);

            if (user == null)
				throw new HttpException((int)HttpStatusCode.NotFound, "The requested member could not be found");

            return user;
        }

        /// <summary>
        /// Details for the user specified by the id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        //[AuthorizeClaim(Resource = SecuredResources.CompanyUserAdministration, Action = SecuredActions.View)]
        public ActionResult Details(Guid id)
        {
            var user = GetUserOr404(id);
            _userContext.DemandAccess(SecuredResources.CompanyUserAdministration, SecuredActions.View, user.Company.Id.Value);

            SetPageMetadata(user, null);
            user.PageLinks = BuildMenu(user);

            return View(user);
        }

        /// <summary>
        /// Containerses the specified id.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="searchCriteria">The search criteria.</param>
        /// <returns></returns>
        //[AuthorizeClaim(Resource = SecuredResources.CompanyUserAdministration, Action = SecuredActions.View)]
        public ActionResult Containers([Bind(Prefix = "id")] Guid userId, SearchCriteria searchCriteria)
        {
            var user = GetUserOr404(userId);
            _userContext.DemandAccess(SecuredResources.CompanyUserAdministration, SecuredActions.View, user.Company.Id.Value);

            if (searchCriteria == null)
                searchCriteria = new SearchCriteria();

            searchCriteria.ApplyContainerSearch(user.Company.CompanyId);
            var searchResults = _searchProvider.SearchContainers<ContainerSearchResult>(searchCriteria);

            var model = new ContainerSearchResultSet(searchResults, _userContext.CanCreateUser(searchCriteria.CompanyId.Value));

            model.SectionId = userId;
            model.PageLinks = BuildMenu(user);
            model.SearchCriteria.PrimaryRefiner = _searchProvider.BuildRefiners(model.PageLinks, model.RefinerResults, searchCriteria, _userContext);

            SetPageMetadata(user, "Containers");
			SetNoSearchResultsMessage(EntityType.User);

            return View(model);
        }

        internal IEnumerable<TaxonomyMenuItem> BuildMenu(UserProfile user)
        {
            var menu = new List<TaxonomyMenuItem>();

            menu.Add(new TaxonomyMenuItem { Key = "basicInformation", Text = "Basic Information", Url = Url.PageUserDetails(user.Id.Value) });
            menu.Add(new TaxonomyMenuItem { Key = EntityType.Container.ToString(), Text = "Container Access", Url = Url.PageUserContainers(user.Id.Value), IsRefinable = true });
            return menu;
        }



        /// <summary>
        /// Edits the user.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns></returns>
        public ActionResult Edit([Bind(Prefix = "id")] Guid userId)
        {
            var user = GetUserOr404(userId);
            _userContext.DemandAccess(SecuredResources.CompanyUserAdministration, SecuredActions.Update, user.Company.Id.Value);

            var userModel = _profileProvider.GetCompanyUserModel(user);

            SetPageMetadata(user, "Edit");
            userModel.PageLinks = BuildMenu(user);

            return View(userModel);
        }

        /// <summary>
        /// Updates the user.
        /// </summary>
        /// <param name="userProfile">The user profile.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Edit(CompanyUserModel userProfile)
        {
            var user = GetUserOr404(userProfile.UserId);
            _userContext.DemandAccess(SecuredResources.CompanyUserAdministration, SecuredActions.Update, userProfile.CompanyId);

            ModelState.Remove("LoginId");
            if (ModelState.IsValid)
            {
                userProfile.AboutMe = user.BasicInformation.AboutMe;
                userProfile.Title = user.BasicInformation.Title;
                _profileProvider.Update(userProfile.UserId, userProfile, Session, _userContext);
                return Redirect(Url.PageCompanyMembers(userProfile.CompanyId));
            }

            SetPageMetadata(user, "Edit");
            userProfile.PageLinks = BuildMenu(user);

            return View(userProfile);
        }

		/// <summary>
		/// Searches users with the specified criteria.
		/// </summary>
		/// <param name="criteria">The criteria.</param>
		/// <returns></returns>
		public JsonResult Search(UserProfileSearchSpecificationModel criteria)
		{
			var users = _profileProvider.Search(criteria);

			return Json(users, JsonRequestBehavior.AllowGet);
		}

        /// <summary>
        /// Displays the user history.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        public ActionResult Audit(Guid id)
        {
            var user = GetUserOr404(id);

            var model = new UserAudit
            {
                Items = _profileProvider.SearchAudit(),
                UserId = id
            };

            SetPageMetadata(user, "Audit Log");
            model.PageLinks = BuildMenu(user);

            return View(model);
        }

        private static string GetClaimDisplayName(string action)
        {
            return CultureInfo.CurrentUICulture.TextInfo.ToTitleCase(action.Substring(action.LastIndexOf('/') + 1).SpaceIt());
        }



        /// <summary>
        /// Shows the form used to update container access for a specified user
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="containerId">The container id.</param>
        /// <returns></returns>
        public ActionResult ContainerAccess([Bind(Prefix = "id")] Guid userId, Guid containerId)
        {
            var user = GetUserOr404(userId);
            _userContext.DemandAccess(SecuredResources.CompanyUserAdministration, SecuredActions.View, user.Company.Id.Value);

            ViewBag.Success = false;
            var model = new UserContainerClaims { UserId = userId, ContainerId = containerId };

            // get all possible claims for a given container
            var actions = _searchProvider.GetAvailableUserClaims(containerId);

            // the white list of claims to use from the super set provided by WCF
            var administerableClaims = new List<string> { SecuredClaims.ContainerEdit, SecuredClaims.ContainerView };
            if (user.IsUlEmployee)
                administerableClaims.Add(SecuredClaims.ContainerPrivate);

            foreach (var claim in actions)
            {
                if (administerableClaims.Contains(claim.Type))
                {
                    bool canAccessAction = ProfileProvider.CanAccessContainer(user, containerId, claim.Type);
                    string displayName = GetClaimDisplayName(claim.Type);

                    model.Claims.Add(new SelectListItem
                    {
                        Value = claim.Type,
                        Text = displayName,
                        Selected = canAccessAction
                    });
                }
            }

            return PartialView(model);
        }

        /// <summary>
        /// Updates access to containers
        /// </summary>
        /// <param name="model">The claims.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult ContainerAccess(UserContainerClaims model)
        {
            ViewBag.Success = false;
            if (!ModelState.IsValid)
                return PartialView(model);

            var userId = model.UserId;
            var containerId = model.ContainerId;
            var profile = _profileProvider.FetchById(userId);
            _userContext.DemandAccess(SecuredResources.CompanyUserAdministration, SecuredActions.Update, profile.Company.Id.Value);

            try
            {
                foreach (var c in model.Claims)
                {
                    c.Text = GetClaimDisplayName(c.Value);
                    var userContainerClaim = _claimFactory.ConstructClaim(new ContainerModel(), containerId, c.Value, userId, profile.BasicInformation.LoginId);

                    if (c.Selected)
                    {
	                    AddBusinessClaim(userContainerClaim, profile);
                    }
                    else
                    {
                        RemoveBusinessClaim(userContainerClaim, profile);
                    }
                }
                AddPageMessage("Your changes have been saved successfully.", title: "Saved", severity: TraceEventType.Start);
                ViewBag.Success = true;
            }
            catch (Exception ex)
            {
                var logMessage = string.Format("Error while editing container access permissions for user {0} and container {1}", userId, containerId);
                _logHelper.Log(MessageIds.UserControllerEditContainerAccessPermissionsException, LogCategory.Container, LogPriority.High, TraceEventType.Error, HttpContext, logMessage);

                var userMessage = "An error has occurred. " + ex.GetBaseException().Message;
                AddPageMessage(userMessage, true, TraceEventType.Error, "Error");
                ModelState.AddModelError("", userMessage);
            }

            return PartialView(model);
        }

        /// <summary>
        /// Makes the admin.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="companyId">The company id.</param>
        /// <returns></returns>
        public ActionResult GrantAdminAccess(Guid userId, Guid companyId)
        {
            _userContext.DemandAccess(SecuredResources.CompanyUserAdministration, SecuredActions.Update, companyId);

            AddCompanyLevelClaim(companyId, userId, SecuredClaims.CompanyAdmin, "users, products and projects");
            return Redirect(Url.PageCompanyUserEdit(userId));
        }

        /// <summary>
        /// Makes the user normal.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="companyId">The company id.</param>
        /// <returns></returns>
        public ActionResult RemoveAdminAccess(Guid userId, Guid companyId)
        {
            _userContext.DemandAccess(SecuredResources.CompanyUserAdministration, SecuredActions.Update, companyId);

			RemoveCompanyLevelClaim(companyId, userId, SecuredClaims.CompanyAdmin, "users, products and projects");
            return Redirect(Url.PageCompanyUserEdit(userId));
        }

        /// <summary>
        /// Grants general site access to a user.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="companyId">The company id.</param>
        /// <returns></returns>
        public ActionResult GrantAccess(Guid userId, Guid companyId)
        {
            _userContext.DemandAccess(SecuredResources.CompanyUserAdministration, SecuredActions.Update, companyId);

            var companyHasAdmin = _profileProvider.DoesCompanyAdminExist(companyId, _userContext);
            if (!companyHasAdmin)
            {
                AddPageMessage("At least one Company Administrator must be assigned before granting access to company users.", true,
                                TraceEventType.Error);
            }
            else
            {
                var profile = _profileProvider.FetchById(userId);

                AddCompanyLevelClaim(companyId, userId, SecuredClaims.CompanyAccess, null);
                //
                // if UL user, grant type admin access to projects & products
                //
                if (profile.IsUlEmployee)
                {
                    AddSystemLevelClaim(userId, SecuredClaims.UlProjectAdministrator, "projects");
                    AddSystemLevelClaim(userId, SecuredClaims.UlProductAdministrator, "products");

                    _emailProvider.PortalAccessGranted(profile.BasicInformation.LoginId);
                }
            }

            return Redirect(Url.PageCompanyUserEdit(userId));
        }

        /// <summary>
        /// Denies the access.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="companyId">The company id.</param>
        /// <returns></returns>
        public ActionResult DenyAccess(Guid userId, Guid companyId)
        {
            _userContext.DemandAccess(SecuredResources.CompanyUserAdministration, SecuredActions.Update, companyId);

            RemoveCompanyLevelClaim(companyId, userId, SecuredClaims.CompanyAccess, null);
            //
            // just in case this was ever a UL user, remove type admin access to projects & products
            //

            //var profile = _profileProvider.FetchById(userId);
            //if (profile.IsUlEmployee)
            //{
                RemoveSystemLevelClaim(userId, SecuredClaims.UlProjectAdministrator, "projects");
                RemoveSystemLevelClaim(userId, SecuredClaims.UlProductAdministrator, "products");
            //}
            return Redirect(Url.PageCompanyUserEdit(userId));
        }

        /// <summary>
        /// Grants the specified user access to all orders in the specififed company
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="companyId">The company id.</param>
        /// <returns></returns>
        public ActionResult GrantOrderAccess(Guid userId, Guid companyId)
        {
            return CheckAndGrantTypeClaim(userId, companyId, SecuredClaims.CompanyOrderAccess, "orders");
        }

        /// <summary>
        /// Remove access for the specified user to all orders in the specififed company
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="companyId">The company id.</param>
        /// <returns></returns>
        public ActionResult RemoveOrderAccess(Guid userId, Guid companyId)
        {
            return RemoveTypeClaim(userId, companyId, SecuredClaims.CompanyOrderAccess, "orders");
        }

        /// <summary>
        /// Grants the specified user access to all projects in the specififed company
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="companyId">The company id.</param>
        /// <returns></returns>
        public ActionResult GrantProjectAccess(Guid userId, Guid companyId)
        {
            return CheckAndGrantTypeClaim(userId, companyId, SecuredClaims.CompanyProjectAccess, "projects");
        }

        /// <summary>
        /// Remove access for the specified user to all projects in the specififed company
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="companyId">The company id.</param>
        /// <returns></returns>
        public ActionResult RemoveProjectAccess(Guid userId, Guid companyId)
        {
            return RemoveTypeClaim(userId, companyId, SecuredClaims.CompanyProjectAccess, "projects");
        }

        /// <summary>
        /// Remove access for the specified user to all projects in the specififed company
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns></returns>
        [AuthorizeClaim(Resource = SecuredResources.AriaAdministration, Action = SecuredActions.Update)]
        public ActionResult RemoveProjectTemplateManagerAccess(Guid userId)
        {
            RemoveSystemLevelClaim(userId, SecuredClaims.UlProjectTemplateAdministrator, "project templates");
            return Redirect(Url.PageCompanyUserEdit(userId));
        }

        /// <summary>
        /// Grants the specified user access to all projects in the specififed company
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <returns></returns>
        [AuthorizeClaim(Resource = SecuredResources.AriaAdministration, Action = SecuredActions.Update)]
        public ActionResult GrantProjectTemplateManagerAccess(Guid userId)
        {
            AddSystemLevelClaim(userId, SecuredClaims.UlProjectTemplateAdministrator, "project templates");
            return Redirect(Url.PageCompanyUserEdit(userId));
        }

        /// <summary>
        /// Grants the product access.
        /// </summary>
        /// <param name="userId">The user unique identifier.</param>
        /// <param name="companyId">The company unique identifier.</param>
        /// <returns></returns>
        public ActionResult GrantProductAccess(Guid userId, Guid companyId)
        {
            return CheckAndGrantTypeClaim(userId, companyId, SecuredClaims.CompanyProductAdmin, "products");
        }

        /// <summary>
        /// Removes the product access.
        /// </summary>
        /// <param name="userId">The user unique identifier.</param>
        /// <param name="companyId">The company unique identifier.</param>
        /// <returns></returns>
        public ActionResult RemoveProductAccess(Guid userId, Guid companyId)
        {
            return RemoveTypeClaim(userId, companyId, SecuredClaims.CompanyProductAdmin, "products");
        }

        /// <summary>
        /// Grants the specified user access to all orders in the entire system
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="companyId">The company id.</param>
        /// <returns></returns>
        public ActionResult GrantOrderAdminAccess(Guid userId, Guid companyId)
        {
            _userContext.DemandAccess(SecuredResources.CompanyUserAdministration, SecuredActions.Update, companyId);

            AddSystemLevelClaim(userId, SecuredClaims.UlOrderAdministrator, "orders");

            return Redirect(Url.PageCompanyUserEdit(userId));
        }

        /// <summary>
        /// Remove access for the specified user to all orders in the entire system
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="companyId">The company id.</param>
        /// <returns></returns>
        public ActionResult RemoveOrderAdminAccess(Guid userId, Guid companyId)
        {
            _userContext.DemandAccess(SecuredResources.CompanyUserAdministration, SecuredActions.Update, companyId);

            RemoveSystemLevelClaim(userId, SecuredClaims.UlOrderAdministrator, "orders");

            return Redirect(Url.PageCompanyUserEdit(userId));
        }





        /// <summary>
        /// Checks the and grant type claim.
        /// </summary>
        /// <param name="userId">The user unique identifier.</param>
        /// <param name="companyId">The company unique identifier.</param>
        /// <param name="claim">The claim.</param>
        /// <param name="typeForMessage">The thing that is being granted access to (plural form).  This will be used in the 'success' message shown to the user.</param>
        /// <returns></returns>
        private ActionResult CheckAndGrantTypeClaim(Guid userId, Guid companyId, string claim, string typeForMessage)
        {
            _userContext.DemandAccess(SecuredResources.CompanyUserAdministration, SecuredActions.Update, companyId);

            var companyHasAdmin = _profileProvider.DoesCompanyAdminExist(companyId, _userContext);
            if (!companyHasAdmin)
                AddPageMessage("At least one Company Administrator must be assigned before granting access to a company's resources.", true, TraceEventType.Error);
            else
                AddCompanyLevelClaim(companyId, userId, claim, typeForMessage);

            return Redirect(Url.PageCompanyUserEdit(userId));
        }

        /// <summary>
        /// Removes the type claim.
        /// </summary>
        /// <param name="userId">The user unique identifier.</param>
        /// <param name="companyId">The company unique identifier.</param>
        /// <param name="claim">The claim.</param>
        /// <param name="typeForMessage">The thing that is being granted access to (plural form).  This will be used in the 'success' message shown to the user.</param>
        /// <returns></returns>
        private ActionResult RemoveTypeClaim(Guid userId, Guid companyId, string claim, string typeForMessage)
        {
            _userContext.DemandAccess(SecuredResources.CompanyUserAdministration, SecuredActions.Update, companyId);

            RemoveCompanyLevelClaim(companyId, userId, claim, typeForMessage);

            return Redirect(Url.PageCompanyUserEdit(userId));
        }

        /// <summary>
        /// Adds the user claim.
        /// </summary>
        /// <param name="companyId">The company id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="claim">The claim.</param>
        /// <param name="typeForMessage">The thing that is being granted access to (plural form).  This will be used in the 'success' message shown to the user.</param>
        internal void AddCompanyLevelClaim(Guid companyId, Guid userId, string claim, string typeForMessage)
        {
            var profile = _profileProvider.FetchById(userId);
            var userBusinessClaim = _claimFactory.ConstructClaim(new CompanyInfo(), companyId, claim, userId, profile.BasicInformation.LoginId);

            _userBusinessClaimProvider.Add(userBusinessClaim);

            //
            // build success message to display to user
            //
            var formatString = string.IsNullOrEmpty(typeForMessage)
                                    ? "<strong>{0}</strong> has been granted access."
                                    : "<strong>{0}</strong> has been granted access to this company's {1}.";
            var message = string.Format(formatString, profile.BasicInformation.LoginId, typeForMessage);
            AddPageMessage(CreatePageMessage(message, title: "Success!", severity: TraceEventType.Information));
        }

        /// <summary>
        /// Adds the system level claim.
        /// </summary>
        /// <param name="userId">The user unique identifier.</param>
        /// <param name="claim">The claim.</param>
        /// <param name="typeForMessage">The thing that is being granted access to (plural form).  This will be used in the 'success' message shown to the user.</param>
        internal void AddSystemLevelClaim(Guid userId, string claim, string typeForMessage)
        {
            var profile = _profileProvider.FetchById(userId);
            var userBusinessClaim = _claimFactory.ConstructSystemLevelClaim(claim, userId, profile.BasicInformation.LoginId);

            _userBusinessClaimProvider.Add(userBusinessClaim);

            //
            // build success message to display to user
            //
            var message = string.Format("<strong>{0}</strong> has been granted access to all {1} in the system.", profile.BasicInformation.LoginId, typeForMessage);
            AddPageMessage(CreatePageMessage(message, title: "Success!", severity: TraceEventType.Information));
        }

		internal bool AddBusinessClaim(UserBusinessClaim userBusinessClaim, UserProfile profile)
		{
			var claims = _userBusinessClaimProvider.GetUserClaimValues(userBusinessClaim.Claim.EntityClaim, profile.BasicInformation.LoginId);
			if (claims == null || !claims.Any(x=> x.Claim != null && x.Claim.EntityClaim == userBusinessClaim.Claim.EntityClaim && x.Claim.Value ==userBusinessClaim.Claim.Value))
			{
				_userBusinessClaimProvider.Add(userBusinessClaim);
			    return true;
			}
			return false;
		}

        /// <summary>
        /// Removes the user claim.
        /// </summary>
        /// <param name="companyId">The company id.</param>
        /// <param name="userId">The user id.</param>
        /// <param name="claim">The claim.</param>
        /// <param name="typeForMessage">The thing that is being granted access to (plural form).  This will be used in the 'success' message shown to the user.</param>
        /// <returns></returns>
        internal UserProfile RemoveCompanyLevelClaim(Guid companyId, Guid userId, string claim, string typeForMessage)
        {
            var profile = _profileProvider.FetchById(userId);
            var userBusinessClaim = _claimFactory.ConstructClaim(new CompanyInfo(), companyId, claim, userId, profile.BasicInformation.LoginId);

            RemoveBusinessClaim(userBusinessClaim, profile);

            //
            // build success message to display to user
            //
            var formatString = string.IsNullOrEmpty(typeForMessage)
                                    ? "<strong>{0}</strong> has been revoked access."
                                    : "<strong>{0}</strong> has been revoked access to this company's {1}, however, any container access granted to specific {1} will remain.";
            var message = string.Format(formatString, profile.BasicInformation.LoginId, typeForMessage);
            AddPageMessage(CreatePageMessage(message, title: "Success!", severity: TraceEventType.Information));

            return profile;
        }

        internal void RemoveSystemLevelClaim(Guid userId, string claim, string typeForMessage)
        {
            var profile = _profileProvider.FetchById(userId);
            var userBusinessClaim = _claimFactory.ConstructSystemLevelClaim(claim, userId, profile.BasicInformation.LoginId);

            var wasRemoved = RemoveBusinessClaim(userBusinessClaim, profile);

            if (wasRemoved)
            {
                //
                // build success message to display to user
                //
                var message = string.Format("<strong>{0}</strong> has been revoked access to all {1} in the system, however, any container access granted to specific {1} will remain.", profile.BasicInformation.LoginId, typeForMessage);
                AddPageMessage(CreatePageMessage(message, title: "Success!", severity: TraceEventType.Information));
            }
        }

        internal bool RemoveBusinessClaim(UserBusinessClaim userBusinessClaim, UserProfile profile)
        {
            var claims = _userBusinessClaimProvider.GetUserClaimValues(userBusinessClaim.Claim.EntityClaim, profile.BasicInformation.LoginId);
            if (claims != null)
            {
                userBusinessClaim = claims.FirstOrDefault(x => x.Claim != null && x.Claim.EntityClaim == userBusinessClaim.Claim.EntityClaim && x.Claim.Value == userBusinessClaim.Claim.Value);
                if (null != userBusinessClaim)
                {
                    _userBusinessClaimProvider.Remove(userBusinessClaim.Claim, userBusinessClaim.Id.Value, profile.Id.Value);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Gets the logging category to use for all logging.
        /// </summary>
        /// <value>
        /// The logging category.
        /// </value>
        protected override LogCategory LoggingCategory
        {
            get { return LogCategory.User; }
        }


        /// <summary>
        /// Sets the page title, crumbs and window title.
        /// </summary>
        /// <param name="user">The company.</param>
        /// <param name="crumbText">The crumb text.</param>
        /// <returns></returns>
        internal IEnumerable<Breadcrumb> SetPageMetadata(UserProfile user, string crumbText)
        {
            var list = new List<Breadcrumb>();

            //
            // add link to company search page if users is entitled to see it
            //
            if (_userContext.IsPortalAdministrator())
            {
                list.Add(new Breadcrumb { Text = "Companies", Url = Url.PageCompany(_userContext) });
            }

            list.Add(new Breadcrumb() { Text = user.Company.Name, Url = Url.PageCompanyDetails(user.Company.Id.Value) });
            list.Add(new Breadcrumb() { Text = "Members", Url = Url.PageCompanyMembers(user.Company.Id.Value) });
            list.Add(new Breadcrumb() { Text = user.BasicInformation.DisplayName, Url = Url.PageUserDetails(user.Id.Value) });

            if (crumbText != null)
                list.Add(new Breadcrumb() { Text = crumbText, Url = Url.Action(null) });

            SetPageMetadata("User", list, trackingTitles: new[] {
				new Pair<string, string>(user.BasicInformation.DisplayName, user.Id.Value.ToString("N")),
				new Pair<string, string>(user.Company.Name, user.Company.Id.Value.ToString("N"))
			});
            if (crumbText != null)
                ViewBag.SearchTitle = crumbText.Pluralize();

            return ViewBag.Breadcrumbs;
        }

    }
}