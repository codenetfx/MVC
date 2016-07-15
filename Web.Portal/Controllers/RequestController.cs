using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.Practices.Unity.Utility;
using UL.Aria.Common;
using UL.Enterprise.Foundation;
using UL.Enterprise.Foundation.Logging;
using UL.Enterprise.Foundation.Mapper;
using UL.Aria.Service.Contracts.Dto;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Profile;
using UL.Aria.Web.Common.Models.Project;
using UL.Aria.Web.Common.Models.Search;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Mvc.Attributes;
using UL.Aria.Web.Common.Providers;
using WebGrease.Css.Extensions;
using System.Collections;
using AutoMapper;

namespace UL.Aria.Web.Portal.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    [Authorize]
    public class RequestController : BaseController
    {
        private readonly IIncomingOrderProvider _incomingOrderProvider;
        private readonly ISearchProvider _searchProvider;
        private readonly ISessionProvider _sessionProvider;
        private readonly IProfileProvider _profileProvider;

        /// <summary>
        /// Projects the controller.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="incomingOrderProvider">The incoming order provider.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="searchProvider">The search provider.</param>
        /// <param name="sessionProvider">The session provider.</param>
        /// <param name="_profileProvider">The _profile provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
        public RequestController(IUserContext userContext, IIncomingOrderProvider incomingOrderProvider,
            IPortalConfiguration portalConfiguration, ILogHelper logHelper,
            ISearchProvider searchProvider, ISessionProvider sessionProvider, IProfileProvider _profileProvider, ITempDataProviderFactory tempDataProviderFactory)
            : base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
        {
            _incomingOrderProvider = incomingOrderProvider;
            _searchProvider = searchProvider;
            _sessionProvider = sessionProvider;
            this._profileProvider = _profileProvider;
        }


        /// <summary>
        ///     Display Incoming Order Details
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [ULEmployee]
        public ActionResult IncomingOrderDetails(Guid id)
        {
            IncomingOrder viewmodel = _incomingOrderProvider.Fetch(id);
            
            viewmodel.PageLinks = HomeController.BuildPageLinks(_userContext, Url, EntityType.InboundOrder.ToString());
            viewmodel.Breadcrumbs = Breadcrumbs(viewmodel, null);
            viewmodel.PageActions = ProjectListingActionsId(id);
            return View(viewmodel);
        }

        /// <summary>
        ///     Orders the queue.
        /// </summary>
        /// <returns></returns>
        [ULEmployee]
        public ActionResult OrderQueue(SearchCriteria criteria, bool useDefaultSearch = false)
        {
            ActionResult defaultRedirect;
            if (ShouldRedirect(useDefaultSearch, _profileProvider, Url.PageOrderQueue(), out defaultRedirect)) return defaultRedirect;

            var setSearchCriteriaAction = new Func<SearchCriteria>(() =>
            {
                criteria.ApplyRequestSearch();
                return criteria;
            });
            criteria = SearchCriteriaExtensions.SetSearchCriteriaFromTempIfNeeded(EntityType.InboundOrder, setSearchCriteriaAction, TempData);

            if (criteria == null)
                return null;

            foreach (var filter in criteria.Filters)
            {
                var list = new List<string>(filter.Value);
                filter.Value.Clear();
                list.ForEach(s => filter.Value.Add(Server.UrlDecode(s)));
            }
            var selectedItems = _sessionProvider.GetGroupItems(EntityType.InboundOrder.ToString());
            SearchResultSet<IncomingOrderSearchResult> model = SearchWithFavorite(criteria, useDefaultSearch, selectedItems);
            model.AppliedFilters.ForEach(x => _searchProvider.FormatRefiner(x, _userContext));
            //model.PageLinks = HomeController.BuildPageLinks(_userContext, Url);

            model.SearchCriteria.PrimaryRefiner = _searchProvider.BuildRefiners(model.PageLinks, model.RefinerResults, criteria, _userContext);
            var refinerSort = new List<string>
            {
                AssetFieldNames.AriaProjectIndustryCode,
                AssetFieldNames.AriaProjectServiceCode, AssetFieldNames.AriaProjectLocationName,
            };

            var taxonomyMenuItem = model.PageLinks.FirstOrDefault(x => x.Key == "InboundOrder");
            if (null != taxonomyMenuItem)
            {
                taxonomyMenuItem.Children.Sort(new SortListComparer(refinerSort));
            }
            
            ViewBag.PageLinks = HomeController.BuildPageLinks(_userContext, Url, EntityType.Project.ToString());
            var jsonSearchCriteria = Mapper.Map<JsonSearchCriteria>(model.SearchCriteria);
            ViewBag.JsonCriteria = jsonSearchCriteria;

            var defaultSearchCriteria = new SearchCriteria()
            {
                EntityType = EntityType.InboundOrder,
                Paging = new Paging() { Page = 1, PageSize = 1 }
            };
            defaultSearchCriteria = defaultSearchCriteria.ApplyRequestSearch();
            var defaultSearchResult = _incomingOrderProvider.Search(defaultSearchCriteria, _userContext, new HashSet<Guid>());
            ViewBag.JsonDefaultCriteria = Mapper.Map<JsonSearchCriteria>(defaultSearchResult.SearchCriteria);

            return View(model);
        }


        /// <summary>
        /// Refinerses the specified criteria.
        /// </summary>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        public ActionResult Refiners(JsonSearchCriteria criteria)
        {
            criteria.Paging.PageSize = 1;
            criteria.Paging.Page = 1;
            criteria.Filters.Where(x => x.Name.StartsWith("$")).ToList().ForEach(x => criteria.Filters.Remove(x));

            var spCriteria = AutoMapper.Mapper.Map<SearchCriteria>(criteria);
            var results = _incomingOrderProvider.Search(spCriteria, _userContext, null);
            var refinerMenu = AutoMapper.Mapper.Map<List<JsonTaxonomyMenuItem>>(results.RefinerResults);

            return Json(refinerMenu);
        }


        private SearchResultSet<IncomingOrderSearchResult> SearchWithFavorite(SearchCriteria criteria, bool useDefaultSearch, HashSet<Guid> selectedItems)
        {
            SearchFavorite favoriteSearch = null;

            string controller = ControllerContext.RouteData.Values["controller"] as String;
            string action = ControllerContext.RouteData.Values["action"] as String;
            var location = @"/" + controller + @"/" + action;

            var availableDefaults = _profileProvider.FetchAvailableDefaultsByLocation(location, _userContext);
            favoriteSearch = availableDefaults.FirstOrDefault(x => x.ActiveDefault);
            SearchResultSet<IncomingOrderSearchResult> model;
            var activeCriteria = criteria;
            if (useDefaultSearch && favoriteSearch != null)
                activeCriteria = favoriteSearch.Criteria;

            model = _incomingOrderProvider.Search(activeCriteria, _userContext, selectedItems);

            model.PageLinks = HomeController.BuildPageLinks(_userContext, Url, model.SearchCriteria.EntityType.ToString());
            model.PageActions = ProjectListingActions();
            model.Breadcrumbs = Breadcrumbs((IncomingOrder)null, null);
            model.SearchCriteria.PrimaryRefiner = _searchProvider.BuildRefiners(model.PageLinks,
                model.RefinerResults, activeCriteria, _userContext);
            model.AppliedFilters.ForEach(x => _searchProvider.FormatRefiner(x, _userContext));
            model.ActiveDefault = favoriteSearch;
            model.AvailableDefaults = availableDefaults;
            model.ShouldShowFilters = true;

            return model;
        }

        internal IEnumerable<Breadcrumb> Breadcrumbs(IncomingOrder order, string crumbText)
        {
            var trackingTitles = new List<Pair<string, string>>();
            var pageName = EntityType.InboundOrder.GetDisplayName();
            var crumbs = new List<Breadcrumb> { new Breadcrumb { Text = pageName.Pluralize(), Url = Url.PageOrderQueue() } };
             
            if (order != null)
            {
                var name = order.ProjectName;
                trackingTitles.Add(new Pair<string, string>(name, order.Id.ToString("N")));
                crumbs.Add(new Breadcrumb { Text = name, Url = Url.PageIncomingOrderDetails(order.Id) });
            }
            else
            {
                //no order => search page, so pluralize the page name
                pageName = pageName.Pluralize();
            }

            if (crumbText != null)
            {
                crumbs.Add(new Breadcrumb { Text = crumbText, Url = Url.Action(null) });
                ViewBag.SearchTitle = crumbText.Pluralize();
            }

            SetPageMetadata(pageName, crumbs, trackingTitles: trackingTitles.ToArray());

            return ViewBag.BreadCrumbs;
        }

        internal List<TaxonomyMenuItem> ProjectListingActions()
        {
            var actionsRight = new List<TaxonomyMenuItem>();

            if (_userContext.CanActOnProject())
            {
                actionsRight.Add(new TaxonomyMenuItem
                {
                    Key = "create",
                    Text = "Create Project",
                    Url = Url.PageOrderCreate(null),
                    Modal = true,
                    CssClass = "arrow primary"
                });
            }

            return actionsRight;
        }

        internal List<TaxonomyMenuItem> ProjectListingActionsId(Guid id)
        {
            var actionsRight = new List<TaxonomyMenuItem>();

            if (_userContext.CanActOnProject())
            {
                actionsRight.Add(new TaxonomyMenuItem
                {
                    Key = "create",
                    Text = "Create Project",
                    Url = Url.PageOrderCreate(id),
                    Modal = true,
                    CssClass = "arrow primary"
                });
            }

            return actionsRight;
        }

        /// <summary>
        /// Gets the logging category to use for all logging.
        /// </summary>
        /// <value>
        /// The logging category.
        /// </value>
        /// <exception cref="System.NotImplementedException"></exception>
        protected override LogCategory LoggingCategory
        {
            get { return LogCategory.Request; }
        }
    }
}
