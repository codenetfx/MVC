using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using UL.Aria.Web.Common.SignalR;
using Microsoft.AspNet.SignalR;

namespace UL.Aria.Web.Portal.Controllers
{
    /// <summary>
    /// Administrative pages for the extranet administrators (i.e. special client users)
    /// </summary>
    //[CompanyAuthorizeClaim(Resource = SecuredResources.CompanyAdministration, Action = SecuredActions.View)]
    public sealed class CompanyController : BaseController
    {
        private const string NotFoundMessage = "The requested company could not be found";
        private readonly ICompanyProvider _companyProvider;
        private readonly IProfileProvider _profileProvider;
        private readonly IContactProvider _contactProvider;
        private readonly ISearchProvider _searchProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyController" /> class.
        /// </summary>
        /// <param name="companyProvider">The provider.</param>
        /// <param name="profileProvider">The profile provider.</param>
        /// <param name="contactProvider">The contact provider.</param>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="searchProvider">The search provider.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
        public CompanyController(ICompanyProvider companyProvider, IProfileProvider profileProvider, IContactProvider contactProvider, 
            IUserContext userContext, ILogHelper logHelper, ISearchProvider searchProvider, IPortalConfiguration portalConfiguration,
            ITempDataProviderFactory tempDataProviderFactory)
            : base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
        {
            _companyProvider = companyProvider;
            _profileProvider = profileProvider;
            _contactProvider = contactProvider;
            _searchProvider = searchProvider;
        }

        /// <summary>
        /// Views a user's company
        /// </summary>
        /// <returns>
        /// ActionResult.
        /// </returns>
        /// <exception cref="System.Web.HttpException">Not authorized to access this company</exception>
	   [CompanyAuthorizeClaim(Resource = SecuredResources.CompanyAdministration, Action = SecuredActions.View)]
		public ActionResult Index()
        {
            if (_userContext.IsPortalAdministrator())
                return Redirect(Url.PageCompanySearch());

            return Details(_userContext.CompanyId.Value);
        }

        /// <summary>
        /// Views the details for a company.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        /// <exception cref="System.Web.HttpException">Not authorized to access this company</exception>
        [CompanyAuthorizeClaim(Resource = SecuredResources.CompanyAdministration, Action = SecuredActions.View, IdentifierName = "id")]
        public ActionResult Details(Guid id)
        {
            var company = _companyProvider.FetchById(id, _userContext);

            ViewBag.PageLinks = BuildMenu(Url, company);
            ViewBag.PageActions = ActionsRight(id, company.CanCreateUser);
            company.Breadcrumbs = SetPageMetadata(company, null);

            return View("Details", company);
        }

        /// <summary>
        /// Edit the details for a company.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        /// <exception cref="System.Web.HttpException">Not authorized to access this company</exception>
        [CompanyAuthorizeClaim(Resource = SecuredResources.CompanyAdministration, Action = SecuredActions.View, IdentifierName = "id")]
        public ActionResult Edit(Guid id)
        {
            var company = _companyProvider.FetchById(id, _userContext);

            ViewBag.PageLinks = BuildMenu(Url, company);
            company.Breadcrumbs = SetPageMetadata(company, null);
            ViewBag.PageActions = new List<TaxonomyMenuItem>();
            return View(company);
        }

        /// <summary>
        /// Edit the details for a company.
        /// </summary>
        /// <param name="companyInfo">The companyInfo.</param>
        /// <returns></returns>
        /// <exception cref="System.Web.HttpException">Not authorized to access this company</exception>
        [CompanyAuthorizeClaim(Resource = SecuredResources.CompanyAdministration, Action = SecuredActions.View, IdentifierName = "id")]
        [HttpPost]
        public ActionResult Edit(CompanyInfo companyInfo)
        {
            _userContext.DemandAccess(SecuredResources.CompanyAdministration, SecuredActions.Update, companyInfo.CompanyId);

            ModelState["ExternalId"].Errors.Clear();
            if (ModelState.IsValid)
            {
                _companyProvider.Update(companyInfo, _userContext);
                return Redirect(Url.PageCompanyDetails(companyInfo.CompanyId));
            }

            ViewBag.PageActions = new List<TaxonomyMenuItem>();
            return View(companyInfo);
        }

        /// <summary>
        /// Containerses the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="searchCriteria">The search criteria.</param>
        /// <returns></returns>
        [CompanyAuthorizeClaim(Resource = SecuredResources.CompanyAdministration, Action = SecuredActions.View, IdentifierName = "id")]
        public ActionResult Containers(Guid? id, SearchCriteria searchCriteria)
        {
            if (id == null)
                throw new HttpException((int)HttpStatusCode.NotFound, NotFoundMessage);

            var company = _companyProvider.FetchById(id.Value, _userContext);
            var model = SearchContainersByCompany(company, searchCriteria);

            //SearchContainersByCompany will also set PageLinks
            ViewBag.PageActions = ActionsRight(model.CompanyId, model.CanCreateUser);
            model.Breadcrumbs = SetPageMetadata(company, "Containers");
            SetNoSearchResultsMessage(EntityType.Company);

            return View("Containers", model);
        }


        /// <summary>
        /// Gets the users.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="searchCriteria">The search criteria.</param>
        /// <returns></returns>
        [CompanyAuthorizeClaim(Resource = SecuredResources.CompanyAdministration, Action = SecuredActions.View, IdentifierName = "id")]
        public ActionResult Members(Guid? id, SearchCriteria searchCriteria)
        {
            searchCriteria.ApplyUserSearch();

            if (id == null)
                id = _userContext.CompanyId;

            if (id == null)
                throw new HttpException((int)HttpStatusCode.NotFound, NotFoundMessage);

            var company = _companyProvider.FetchById(id.Value, _userContext);
            var companyUsersModel = _profileProvider.SearchCompanyUsers(id.Value, searchCriteria, _userContext);

            ViewBag.PageLinks = BuildMenu(Url, company);
            ViewBag.PageActions = ActionsRight(companyUsersModel.CompanyId, _userContext.CanCreateUser(id.Value));
            companyUsersModel.Breadcrumbs = SetPageMetadata(company, "Members");
            SetNoSearchResultsMessage(EntityType.Company);

            return View(companyUsersModel);
        }

        /// <summary>
        /// Searches companies with the specified criteria.
        /// </summary>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        [AuthorizeClaim(Resource = SecuredResources.AriaAdministration, Action = SecuredActions.View)]
        public JsonResult Search(SearchCriteria criteria)
        {
            criteria = criteria.ApplyCompanySearch();
            var companies = _companyProvider.Search(criteria, _userContext);

            return Json(companies.Results, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Removes the user.
        /// </summary>
        /// <param name="userId">The user id.</param>
        /// <param name="companyId">The company id.</param>
        /// <returns></returns>
        [HttpPost]
		[CompanyAuthorizeClaim(Resource = SecuredResources.CompanyAdministration, Action = SecuredActions.View)]
		public JsonResult RemoveUser(Guid userId, Guid companyId)
        {
            GrowlMessage message = null;
            bool success = false;

            try
            {
                _userContext.DemandAccess(SecuredResources.CompanyUserAdministration, SecuredActions.Create, companyId);
                var user = _profileProvider.FetchById(userId);
                if (null != user)
                {
                    bool isCompanyAdmin = ProfileProvider.IsCompanyAdmin(user, companyId);

                    if (!isCompanyAdmin)
                        _profileProvider.Delete(userId);
                    else
                        throw new Exception("Company administrators may not be deleted.");
                }

                success = true;
                var localMessage = CreatePageMessage("User has been deleted succesfully.", title: "Success!", severity: TraceEventType.Start);

                AddPageMessage(localMessage);

            }
            catch (Exception exception)
            {
                _logHelper.Log(MessageIds.CompanyControllerDeleteUserException, LogCategory.User, LogPriority.Critical, TraceEventType.Error, ControllerContext.HttpContext, "Error removing user " + userId, exception);

                var msg = "There was an error deleting the user: " + exception.GetBaseException().Message;
                message = CreatePageMessage(msg, title: "Error", severity: TraceEventType.Error);
                AddPageMessage(message);
            }


            return Json(new
            {
                success,
                message
            });
            //  return RedirectToAction("Members", new { id = companyId });
        }


        /// <summary>
        /// Removes a company
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
		[CompanyAuthorizeClaim(Resource = SecuredResources.CompanyAdministration, Action = SecuredActions.View)]
		public JsonResult Remove(Guid id)
        {
            GrowlMessage message;
            bool success;

            if (!_userContext.CanAccess(SecuredResources.CompanyAdministration, SecuredActions.Update, id))
            {
                message = CreatePageMessage("You do not have access to delete this company", title: "Error", severity: TraceEventType.Error);
				AddPageMessage(message);
				success = false;
            }
            else
            {
                _companyProvider.Delete(id);
                message = CreatePageMessage("This company has been deleted successfully.", title: "Success!", severity: TraceEventType.Start);
				AddPageMessage(message);
				success = true;
            }

            return Json(new
            {
                success = success,
                message = message
            });
        }

        /// <summary>
        /// Creates the company.
        /// </summary>
        /// <param name="companyInfo">The company info.</param>
        /// <returns></returns>
        [HttpPost]
		[CompanyAuthorizeClaim(Resource = SecuredResources.CompanyAdministration, Action = SecuredActions.View)]
		public ActionResult Create(CompanyInfo companyInfo)
        {
            _userContext.DemandAccess(SecuredResources.CompanyAdministration, SecuredActions.Create, companyInfo.CompanyId);
            if (ModelState.IsValid)
            {
                _companyProvider.Create(companyInfo, _userContext);

                return Redirect(Url.PageCompanySearch());
            }

            BuildCreateModel(companyInfo);

            return View(companyInfo);
        }

        /// <summary>
        /// Creates the company.
        /// </summary>
        /// <returns></returns>
		[CompanyAuthorizeClaim(Resource = SecuredResources.CompanyAdministration, Action = SecuredActions.View)]
		public ActionResult Create()
        {
            var id = Guid.NewGuid();
            var companyInfo = new CompanyInfo() { Id = id, Name = "New" };
            BuildCreateModel(companyInfo);

            return View(companyInfo);
        }

        private void BuildCreateModel(CompanyInfo companyInfo)
        {
            companyInfo.PageLinks = HomeController.BuildPageLinks(_userContext, Url, EntityType.Company.ToString());
            companyInfo.PageActions = ActionsRight(companyInfo.Id.Value, true);
            companyInfo.Breadcrumbs = SetPageMetadata(null, "Create");

            companyInfo.AttachSessionStash(_userContext.SessionStash);
        }



        /// <summary>
        /// Adds the new user.
        /// </summary>
        /// <param name="companyId">The id.</param>
        /// <returns></returns>
        [AuthorizeClaim(Resource = SecuredResources.CompanyUserAdministration, Action = SecuredActions.Create, IdentifierName = "id")]
        public ActionResult CreateUser([Bind(Prefix = "id")] Guid companyId)
        {
            var addModel = new CompanyUserModel();
            addModel.CompanyId = companyId;
            var company = _companyProvider.FetchById(companyId, _userContext);
            addModel.CompanyExternalId = company.ExternalId;
            if (!string.IsNullOrEmpty(addModel.CompanyExternalId))
            {
                addModel.CompanyExternalId = addModel.CompanyExternalId.Split(',')[0].Trim();
            }
            addModel.PageLinks = BuildMenu(Url, addModel);

            SetPageMetadata(company, "Add Member");
            ViewBag.PageActions = new List<TaxonomyMenuItem>();
            return View(addModel);
        }

        /// <summary>
        /// Creates the user.
        /// </summary>
        /// <param name="companyId">The company id.</param>
        /// <param name="userInfo">The user info.</param>
        /// <returns></returns>
        [HttpPost]
        [AuthorizeClaim(Resource = SecuredResources.CompanyUserAdministration, Action = SecuredActions.Create, IdentifierName = "id")]
        public ActionResult CreateUser([Bind(Prefix = "id")] Guid companyId, CompanyUserModel userInfo)
        {
            if (ModelState.IsValid)
            {
                _profileProvider.Create(userInfo, userInfo.CompanyId, _userContext);

                return Redirect(Url.PageCompanyMembers(userInfo.CompanyId));
            }

            userInfo.PageLinks = BuildMenu(Url, userInfo);
            ViewBag.PageActions = new List<TaxonomyMenuItem>();
            var company = _companyProvider.FetchById(userInfo.CompanyId, _userContext);
            SetPageMetadata(company, "Add Member");

            return View(userInfo);
        }

      
        /// <summary>
        /// Updates the order contacts for all valid projects/requests matching order number.
        /// </summary>
        /// <param name="orderNumber">The order number.</param>
        /// <returns></returns>
        [ULEmployee]
        public ActionResult UpdateOrderContacts(string orderNumber)
        {
            var model = true;
            try
            {
                _contactProvider.UpdateContactsForOrder(orderNumber);
                
            }
            catch (Exception)
            {
               model= false;
            }
            return PartialView(model);
        }
      
        /// <summary>
        /// Gets account Info for external Id or list of external ids
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public JsonResult GetAccounts(CompanyContactRequest request)
        {
            var model = new CompanyModel();
            try
            {
                
                if (!string.IsNullOrEmpty(request.AccountNumber))
                {
                    //external ids already recieved in inital request
                    model = _companyProvider.GetAccountInformation(request.AccountNumber);
                }
                else
                {
                    //Need to fetch the external ids first  
                    var companyId = request.CompanyId.ParseOrDefault<Guid>(Guid.Empty);
                    var companyInfo = _companyProvider.FetchById(companyId, _userContext);
                    if (companyInfo == null)
                    {
                        model.Successful = false;
                        model.ErrorCode = 400;
                    }
                    else
                    {
                        var externIds = companyInfo.ExternalId.Split(new char[] { ',' }).Select(x=>x.Trim(new char[] {' '})).ToList();
                        model.AccountIds = externIds;
                        model.Successful = true;
                        model.ErrorCode = 200;
                    }
                }
            }
            catch (Exception ex)
            {
                ex.ToLogMessage(MessageIds.CustomerSyncWebRequestException, LogCategory.Company, LogPriority.Medium,TraceEventType.Error);
                model.ErrorCode = 500;
                model.Successful = false;
                model.Message = "Server encounter an error.";
            }

            return Json(model, new Newtonsoft.Json.JsonSerializerSettings());

        }

        /// <summary>
        /// Gets the logging category to use for all logging.
        /// </summary>
        /// <value>
        /// The logging category.
        /// </value>
        protected override LogCategory LoggingCategory
        {
            get { return LogCategory.Company; }
        }

        private ContainerSearchResultSet SearchContainersByCompany(CompanyInfo company, SearchCriteria searchCriteria)
        {
            if (company == null || company.Id == null)
                throw new HttpException((int)HttpStatusCode.NotFound, NotFoundMessage);

            var companyId = company.Id.Value;

            searchCriteria.ApplyContainerSearch(companyId);

            var resultSet = _searchProvider.SearchContainers<ContainerSearchResult>(searchCriteria);

            var model = new ContainerSearchResultSet(resultSet, _userContext.CanCreateUser(companyId));
            model.CompanyId = companyId;
            ViewBag.PageLinks = BuildMenu(Url, company);
            model.SearchCriteria.PrimaryRefiner = _searchProvider.BuildRefiners(ViewBag.PageLinks, model.RefinerResults, searchCriteria, _userContext);

            return model;
        }

        /// <summary>
        /// Actions the right.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="canCreateUser">if set to <c>true</c> [can create user].</param>
        /// <returns></returns>
        internal List<TaxonomyMenuItem> ActionsRight(Guid id, bool canCreateUser)
        {
            var actionsRight = new List<TaxonomyMenuItem>();

            if (canCreateUser)
            {
                actionsRight.Add(new TaxonomyMenuItem
                {
                    Key = "addMember",
                    Text = "Add Member",
                    Url = Url.PageCompanyUserCreate(id),
                    LinkData = { { "class", "arrow" } }
                });

                actionsRight.Add(new TaxonomyMenuItem
                {
                    Key = "editCompany",
                    Text = "Edit Company",
                    Url = Url.PageCompanyEdit(id),
                    LinkData = { { "class", "arrow"} }
                });
            }
            return actionsRight;
        }

        /// <summary>
        /// Breadcrumbses the specified dictionary.
        /// </summary>
        /// <param name="company">The company.</param>
        /// <param name="crumbText">The crumb text.</param>
        /// <returns></returns>
        internal IEnumerable<Breadcrumb> SetPageMetadata(CompanyInfo company, string crumbText)
        {
            var list = new List<Breadcrumb>();

            //
            // add link to company search page if users is entitled to see it
            //
            if (_userContext.IsPortalAdministrator())
                list.Add(new Breadcrumb { Text = "Companies", Url = Url.PageCompany(_userContext) });

            if (company != null)
                list.Add(new Breadcrumb() { Text = company.Name, Url = Url.PageCompanyDetails(company.Id.Value) });

            if (crumbText != null)
                list.Add(new Breadcrumb() { Text = crumbText, Url = Url.Action(null) });

            var tracking = new List<Pair<string, string>>(1);
            if (company != null)
                tracking.Add(new Pair<string, string>(company.Name, company.Id.Value.ToString("N")));

            SetPageMetadata("Company", list, trackingTitles: tracking.ToArray());

            if (company != null)
            {
                //TODO: move this to a common model/interface for these pages
                ViewBag.InstanceName = company.Name;
                ViewBag.InstanceDescription = company.Description;
            }

            if (crumbText != null)
                ViewBag.SearchTitle = crumbText.Pluralize();

            return ViewBag.Breadcrumbs;
        }

        /// <summary>
        /// Builds the menu.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="company">The company.</param>
        /// <returns></returns>
        internal static IEnumerable<TaxonomyMenuItem> BuildMenu(UrlHelper url, ICompanyInfo company)
        {
            var menu = new List<TaxonomyMenuItem>();

            menu.Add(new TaxonomyMenuItem { Key = "basicInformation", Text = "Basic Information", Url = url.PageCompanyDetails(company.CompanyId) });
            menu.Add(new TaxonomyMenuItem { Key = "members", Text = "Members", Url = url.PageCompanyMembers(company.CompanyId) });
            menu.Add(new TaxonomyMenuItem { Key = EntityType.Container.ToString(), Text = "Containers", IsRefinable = true, Url = url.PageCompanyContainers(company.CompanyId) });

            return menu;
        }


       
    }
}
