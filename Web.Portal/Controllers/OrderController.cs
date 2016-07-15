using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;
using Microsoft.Practices.Unity.Utility;
using UL.Aria.Common;
using UL.Enterprise.Foundation;
using UL.Aria.Common.Authorization;
using UL.Enterprise.Foundation.Logging;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Container;
using UL.Aria.Web.Common.Models.Order;
using UL.Aria.Web.Common.Models.Search;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Providers;

namespace UL.Aria.Web.Portal.Controllers
{
    /// <summary>
    /// Order Controller
    /// </summary>
    [Authorize]
    public sealed class OrderController : BaseController
    {
        private readonly ISearchProvider _searchProvider;
        private readonly IOrderProvider _orderProvider;
        private readonly ISessionProvider _sessionProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="OrderController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="searchProvider">The search provider.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="orderProvider">The order provider.</param>
        /// <param name="sessionProvider">The session provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
        public OrderController(IUserContext userContext, ILogHelper logHelper, ISearchProvider searchProvider, IPortalConfiguration portalConfiguration,
            IOrderProvider orderProvider, ISessionProvider sessionProvider, ITempDataProviderFactory tempDataProviderFactory)
            : base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
        {
            _searchProvider = searchProvider;
            _orderProvider = orderProvider;
            _sessionProvider = sessionProvider;
        }

        private OrderDetail GetOrderOr404(Guid id)
        {
            var order = _orderProvider.FetchById(id, _userContext);

            if (order == null)
                throw new HttpException((int)HttpStatusCode.NotFound, "The requested order could not be found");

			if(order.ContainerId == Guid.Empty)
				//should no longer get here now that wcf will populate
				order.ContainerId = _searchProvider.GetContainerId(id, _sessionProvider, _userContext) ?? Guid.Empty;

            _userContext.DemandAccess(SecuredResources.OrderInstance, SecuredActions.View, order.ContainerId);
            return order;
        }

        /// <summary>
        /// Indexes the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public ActionResult Index(Guid id)
        {
            var orderVm = GetOrderOr404(id);

            int? documentsCount = null;

			//
			// Refiner SP search for asset types, scoped to this order
			//
            var criteria = new SearchCriteria().ApplyOrderFilter(orderVm.Id)
                                                .ApplyDocumentSearch()
                                                .ApplyContainerFilter(orderVm.ContainerId);

            documentsCount = (int?)_searchProvider.FetchDocuments(criteria, _sessionProvider, _userContext).Results.Count;

            //
			// fetch all services on this order to get the count
			//
			criteria = new SearchCriteria().ApplyOrderFilter(id).ApplyOrderServicesSearch();
			var services = _orderProvider.SearchOrderServices(criteria, orderVm, _userContext);

			//
			// build view model, passing in counts
			//
            orderVm.PageActions = ActionsRight(orderVm);
            orderVm.PageLinks = ActionsLeft(id, documentsCount, services.SearchCriteria.Paging.TotalResults);
            orderVm.Breadcrumbs = Breadcrumbs(orderVm, null);

            return View(orderVm);
        }

        /// <summary>
        /// Details the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        public ActionResult Details(Guid id)
        {
            var order = GetOrderOr404(id);

            order.PageLinks = ActionsLeft(id);
            order.Breadcrumbs = Breadcrumbs(order, "Order Details");
            order.PageActions = ActionsRight(order);

            return View(order);
        }

        /// <summary>
        /// Order Customer Information
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult CustomerInformation(Guid id)
        {
            var order = GetOrderOr404(id);
            order.Customer.UlContactEmail = IsValidEmail(order.Customer.UlContact) ? order.Customer.UlContact : string.Empty;

            order.PageLinks = ActionsLeft(id);
            order.Breadcrumbs = Breadcrumbs(order, "Customer Information");
            order.PageActions = ActionsRight(order);

            return View(order);
        }

        /// <summary>
        /// Documents the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        public ActionResult Documents(Guid id, SearchCriteria criteria)
        {
            var order = GetOrderOr404(id);

            var documentVm = new OrderDocuments(order);
            documentVm.PageLinks = ActionsLeft(id);
            documentVm.Breadcrumbs = Breadcrumbs(order, "Documents");
            documentVm.PageActions = ActionsRight(order);

            criteria.ApplyContainerFilter(order.ContainerId).ApplyDocumentSearch();
            
			documentVm.DocumentResults = _searchProvider.FetchDocuments(criteria, _sessionProvider, _userContext);
            documentVm.DocumentResults.SearchCriteria.PrimaryRefiner = _searchProvider.BuildRefiners(documentVm.PageLinks, documentVm.DocumentResults.RefinerResults, criteria, _userContext);

			SetNoSearchResultsMessage(EntityType.Order);

            return View(documentVm);
        }

        /// <summary>
        /// Services the specified identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        public ActionResult Services(Guid id, SearchCriteria criteria)
        {
            var order = GetOrderOr404(id);
	        criteria.ApplyOrderServicesSearch().ApplyOrderFilter(id);

            var orderServicesVm = new OrderServices(order);
            orderServicesVm.OrderServicesSearchResult = _orderProvider.SearchOrderServices(criteria, order, _userContext);
            orderServicesVm.PageLinks = ActionsLeft(id);
            orderServicesVm.Breadcrumbs = Breadcrumbs(order, "Services");
            orderServicesVm.PageActions = ActionsRight(order);

			SetNoSearchResultsMessage(EntityType.Order);
            return View(orderServicesVm);
        }

        ///// <summary>
        ///// Samples the tests.
        ///// </summary>
        ///// <param name="id">The id.</param>
        ///// <returns></returns>
        //public ActionResult SamplesTests(Guid id)
        //{
        //    var order = GetOrderOr404(id);

        //    order.PageLinks = ActionsLeft(id);
        //    order.Breadcrumbs = Breadcrumbs(order, "Samples & Tests");
        //    order.PageActions = ActionsRight(order);

        //    return View(order);
        //}

        /// <summary>
        /// Gets the logging category to use for all logging.
        /// </summary>
        /// <value>
        /// The logging category.
        /// </value>
        protected override LogCategory LoggingCategory
        {
            get { return LogCategory.Order; }
        }

        internal List<TaxonomyMenuItem> ActionsLeft(Guid id, int? documentCount = null, long? servicesCount = null)
        {
            return new List<TaxonomyMenuItem> {
				new TaxonomyMenuItem { Key = "overview",  Text = "Overview", Url = Url.OrderOverview(id), LinkData = {{"class", "empty"}} },
                new TaxonomyMenuItem { Key = "Order",  Text = "Order Details", Url = Url.OrderDetails(id), LinkData = {{"class", "empty"}}},
                new TaxonomyMenuItem { Key = "orderServices", Text = "Services", Url = Url.OrderServices(id), IsRefinable = false, Count = servicesCount },
				new TaxonomyMenuItem { Key = EntityType.Document.ToString() /* don't change this key or refiners will disappear */,  Text = "Documents", Url = Url.OrderDocuments(id), IsRefinable = true, Count =  documentCount.GetValueOrDefault()},
			};
        }


        internal List<TaxonomyMenuItem> ActionsRight(OrderDetail order)
        {
            var actionsRight = new List<TaxonomyMenuItem>();

            if (!order.IsReadOnly)
            {
                actionsRight.Add(new TaxonomyMenuItem
                {
                    Key = "upload",
                    Modal = true,
                    Text = "Upload Documents",
                    Url = Url.UploadMultipleDocuments(order.Id, EntityType.Order, order.ContainerId),
                    CssClass = "primary arrow",
                    ModalWidth = TaxonomyMenuItem.UploadModalWidth
                });
            }

            return actionsRight;
        }
        internal IEnumerable<Breadcrumb> Breadcrumbs(OrderDetail order, string crumbText)
        {
            var orderName = string.Format("Order #{0}", order.OrderNumber);

            var crumbs = new List<Breadcrumb> {
				new Breadcrumb { Text = "Orders", Url = Url.PageSearchOrders() },
				new Breadcrumb { Text =  orderName, Url = Url.OrderOverview(order.Id) }
			};

            if (crumbText != null)
            {
                crumbs.Add(new Breadcrumb { Text = crumbText, Url = Url.Action(null) });
                ViewBag.SearchTitle = crumbText.Pluralize();
            }

            SetPageMetadata("Order", crumbs, trackingTitles: new Pair<string, string>(orderName, order.Id.ToString("N")));

            return ViewBag.BreadCrumbs;
        }
        internal bool IsValidEmail(string email)
        {
            try
            {
                new MailAddress(email);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
