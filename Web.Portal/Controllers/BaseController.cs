using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Web.Configuration;
using System.Web.Mvc;
using Microsoft.Practices.Unity.Utility;
using UL.Aria.Web.Portal.Logging;
using UL.Enterprise.Foundation;
using UL.Enterprise.Foundation.Logging;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Search;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Providers;
using System.Web;
using Harbour.RedisTempData;
namespace UL.Aria.Web.Portal.Controllers
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class BaseController : Controller
    {
        /// <summary>
        /// The temp data service call key
        /// </summary>
        public const string PageMessagesKey = "PageMessages";

        /// <summary>
        /// The temp data service call exception key
        /// </summary>
        public const string TempDataServiceCallExceptionKey = "ServiceCallException";

        /// <summary>
        /// The format string used to generate "no results" mesages when searchign for an entity type filtered to an asset, i.e. searching for a product's documents.
        /// </summary>
        protected const string NoAssetSearchResultsFormatString = "There are no {{0}} associated with this {0}.";

        /// <summary>
        /// The user context instance
        /// </summary>
        protected readonly IUserContext _userContext;

        /// <summary>
        /// The log helper
        /// </summary>
        protected readonly ILogHelper _logHelper;

        /// <summary>
        /// The portal configuration
        /// </summary>
        protected readonly IPortalConfiguration _portalConfiguration;
        private  readonly ITempDataProviderFactory _tempDataProviderFactory;


        /// <summary>
        /// Initializes a new instance of the <see cref="BaseController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
        protected BaseController(IUserContext userContext, ILogHelper logHelper, IPortalConfiguration portalConfiguration, 
            ITempDataProviderFactory tempDataProviderFactory)
        {
            _userContext = userContext;
            _logHelper = logHelper;
            _portalConfiguration = portalConfiguration;
            _tempDataProviderFactory = tempDataProviderFactory;
            AssureTempDataRedisProvider();
        }

        /// <summary>
        /// Assures the temporary data provider.
        /// </summary>
        internal void AssureTempDataRedisProvider(){
            if (_tempDataProviderFactory != null 
                && _portalConfiguration.EnableRedisTempDataSupport)
            {
                TempDataProvider = _tempDataProviderFactory.GetProvider();
            }
        }

        /// <summary>
        /// Gets the portal version.
        /// </summary>
        /// <value>
        /// The portal version.
        /// </value>
        public static string PortalVersion
        {
            get { return Assembly.GetExecutingAssembly().FormatVersion(); }
        }

        /// <summary>
        /// Creates a <see cref="T:System.Web.Mvc.JsonResult" /> object that serializes the specified object to JavaScript Object Notation (JSON) format.
        /// </summary>
        /// <param name="data">The JavaScript object graph to serialize.</param>
        /// <param name="contentType">The content type (MIME type).</param>
        /// <param name="contentEncoding">The content encoding.</param>
        /// <returns>
        /// The JSON result object that serializes the specified object to JSON format.
        /// </returns>
        protected override JsonResult Json(object data, string contentType, Encoding contentEncoding)
        {
            return new JsonNetResult()
            {
                Data = data,
                ContentType = contentType,
                ContentEncoding = contentEncoding,
            };
        }

        /// <summary>
        /// Creates a <see cref="T:System.Web.Mvc.JsonResult" /> object that serializes the specified object to JavaScript Object Notation (JSON) format.
        /// </summary>
        /// <param name="data">The JavaScript object graph to serialize.</param>
        /// <param name="contentType">The content type (MIME type).</param>
        /// <param name="contentEncoding">The content encoding.</param>
        /// <param name="jsonRequestBehavior">The json request behavior.</param>
        /// <returns>
        /// The JSON result object that serializes the specified object to JSON format.
        /// </returns>
        protected override JsonResult Json(object data, string contentType, Encoding contentEncoding, JsonRequestBehavior jsonRequestBehavior)
        {
            return new JsonNetResult()
            {
                Data = data,
                ContentType = contentType,
                ContentEncoding = contentEncoding,
                JsonRequestBehavior = jsonRequestBehavior
            };
        }

        /// <summary>
        /// returns a jsonNet Serialization result allowing ro the json settings to be overridden.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <param name="jsonSettings">The json settings.</param>
        /// <param name="jsonRequestBehavior">The json request behavior.</param>
        /// <returns></returns>
        protected virtual JsonResult Json(object data, Newtonsoft.Json.JsonSerializerSettings jsonSettings,
            JsonRequestBehavior jsonRequestBehavior = JsonRequestBehavior.DenyGet)
        {
            var result = this.Json(data, jsonRequestBehavior);
            (result as JsonNetResult).JsonSettings = jsonSettings;
            return result;
        }



        /// <summary>
        /// Gets the logging category to use for all logging.
        /// </summary>
        /// <value>
        /// The logging category.
        /// </value>
        protected abstract LogCategory LoggingCategory { get; }

        /// <summary>
        /// Called before the action result that is returned by an action method is executed.
        /// </summary>
        /// <param name="filterContext">Information about the current request and action result</param>
        protected override void OnResultExecuting(ResultExecutingContext filterContext)
        {
            // TempData lives until it is read, so this clears it out to meet Aria requirements.
            if (TempData.ContainsKey("Criteria"))
            {
                var hackForClearingCriteria = TempData["Criteria"];
            }

            var view = filterContext.Result as ViewResultBase;
            SetCommonViewBagProperties(filterContext.Controller.ViewBag);
            SetCommonViewModelProperties(view);
            base.OnResultExecuting(filterContext);
        }

        /// <summary>
        /// Sets the common view bag properties used by every page
        /// </summary>
        /// <param name="viewBag">The view bag.</param>
        private void SetCommonViewBagProperties(dynamic viewBag)
        {
            viewBag.PortalConfiguration = _portalConfiguration;
            viewBag.SessionStash = _userContext.SessionStash;
            viewBag.UlApps = _portalConfiguration.UlApps;
			ViewBag.IsUlEmployee = _userContext.IsUlEmployee;
            if (viewBag.PageLinks == null)
            {
                viewBag.PageLinks = HomeController.BuildPageLinks(_userContext, Url, string.Empty);
            }
        }

        /// <summary>
        /// Sets the "no results" message for search pages based on the provided entity type.
        /// </summary>
        /// <param name="entityType">Type of the entity.</param>
        protected void SetNoSearchResultsMessage(EntityType entityType)
        {
            ViewBag.NoResultsMessage = string.Format(NoAssetSearchResultsFormatString, entityType.GetDisplayName().ToLower());
        }


        /// <summary>
        /// Adds the page message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="sticky">if set to <c>true</c> [sticky].</param>
        /// <param name="severity">The severity.</param>
        /// <param name="title">The title.</param>
        protected void AddPageMessage(string message, bool sticky = false, TraceEventType severity = TraceEventType.Information, string title = null)
        {
            var growl = CreatePageMessage(message, sticky, severity, title);

            AddPageMessage(growl);
        }

        /// <summary>
        /// Creates the grown page message that can be then serialized to a javascript alert for client side messages
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="sticky">if set to <c>true</c> [sticky].</param>
        /// <param name="severity">The severity.</param>
        /// <param name="title">The title.</param>
        /// <returns></returns>
        public GrowlMessage CreatePageMessage(string message, bool sticky = false, TraceEventType severity = TraceEventType.Information, string title = null)
        {
            var growl = new GrowlMessage { text = message, sticky = sticky, title = title };

            switch (severity)
            {
                case TraceEventType.Information:
                    growl.image = Url.Content("~/content/img/icons/info-48.png"); break;
                case TraceEventType.Warning:
                    growl.image = Url.Content("~/content/img/icons/warn-48.png"); break;
                case TraceEventType.Start:
                    growl.image = Url.Content("~/content/img/icons/start-48.png"); break;
                case TraceEventType.Stop:
                    growl.image = Url.Content("~/content/img/icons/stop-48.png"); break;
                default:
                    growl.image = Url.Content("~/content/img/icons/error-48.png"); break;
            }

            // log as Low for reference
            _logHelper.Log(MessageIds.BaseControllerPageGrowlMessage, LoggingCategory, LogPriority.Low, severity, HttpContext, message);

            return growl;
        }

        /// <summary>
        /// Adds the page message.
        /// </summary>
        /// <param name="message">The message.</param>
        protected void AddPageMessage(GrowlMessage message)
        {
            var messages = (List<GrowlMessage>)TempData[PageMessagesKey];

            if (messages == null)
            {
                TempData[PageMessagesKey] = messages = new List<GrowlMessage>();
            }

            messages.Add(message);
        }

        /// <summary>
        /// Calls the service and redirects to Action on success.  Logs success and failure messages appropriately.
        /// Renders current view with model on failure.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <param name="serviceCall">The service call.</param>
        /// <param name="successMessage">The success message.</param>
        /// <param name="failureMessage">The failure message format strign where 0 = base exception message.</param>
        /// <returns></returns>
        protected ActionResult TryServiceCall(object model, Action serviceCall, string successMessage, string failureMessage)
        {
            try
            {
                serviceCall();
                AddPageMessage(successMessage, severity: TraceEventType.Information, title: "Success!");
                TempData[TempDataServiceCallExceptionKey] = null;

                return RedirectToAction(ControllerContext.RouteData.GetRequiredString("action"));
            }
            catch (Exception ex)
            {
                failureMessage = String.Format(failureMessage, ex.GetBaseException().Message);

                _logHelper.Log(MessageIds.BaseControllerServiceError, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext, failureMessage, ex);

                AddPageMessage(failureMessage, true, TraceEventType.Error, title: "Error");
                TempData[TempDataServiceCallExceptionKey] = ex;

                return View(model);
            }
        }


        /// <summary>
        /// Renders the partial view to string.
        /// </summary>
        /// <param name="viewName">Name of the view.</param>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        protected string RenderPartialViewToString(string viewName, object model)
        {
            if (String.IsNullOrEmpty(viewName))
                viewName = ControllerContext.RouteData.GetRequiredString("action");

            ViewEngineResult viewResult = ViewEngineCollection.FindPartialView(ControllerContext, viewName);

            return RenderViewEngineToString(viewResult, model);
        }


        /// <summary>
        /// Renders the partial view to string.
        /// </summary>
        /// <param name="viewName">Name of the view.</param>
        /// <param name="layoutPage">The layout page.</param>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        protected string RenderViewToString(string viewName, string layoutPage, object model)
        {
            if (String.IsNullOrEmpty(viewName))
                viewName = ControllerContext.RouteData.GetRequiredString("action");

            ViewEngineResult viewResult = ViewEngineCollection.FindView(ControllerContext, viewName, layoutPage);

            return RenderViewEngineToString(viewResult, model);
        }

        private string RenderViewEngineToString(ViewEngineResult viewResult, object model)
        {
            ViewData.Model = model;
            SetCommonViewBagProperties(ViewBag);

            using (StringWriter sw = new StringWriter())
            {
                ViewContext viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw);
                viewResult.View.Render(viewContext, sw);
                return sw.GetStringBuilder().ToString();
            }
        }


        /// <summary>
        /// Sets various ViewBag properties relating to page titles, window titles, bread crumbs and SEO related tags.
        /// </summary>
        /// <param name="pageTitle">The page title.</param>
        /// <param name="crumbs">The breadcrumbs.  Note you do not need to include the crumb for Home.</param>
        /// <param name="seoDescription">The seo description.</param>
        /// <param name="seoKeywords">The seo keyowrds.</param>
        /// <param name="trackingTitles">A list of secure window title replacements that should be used for web analytics tracking.  The First is the text to replace with the Second.</param>
        protected virtual void SetPageMetadata(string pageTitle, IEnumerable<Breadcrumb> crumbs, string seoDescription = null, string seoKeywords = null, params Pair<string, string>[] trackingTitles)
        {
            //
            // build window title based on bread crumbs (not including Home)
            //
            var windowTitle = new StringBuilder();
            var breadcrumbs = new List<Breadcrumb>(crumbs);
            foreach (var breadcrumb in breadcrumbs)
            {
                windowTitle.Insert(0, " :: ");
                windowTitle.Insert(0, breadcrumb.Text);
            }
            windowTitle.Append("UL");

            // add Home link to crumbs
            var home = new Breadcrumb() { Url = Url.PageHome(), Text = "Home" };
            breadcrumbs.Insert(0, home);

            //
            // assign all common ViewBag properties
            //
            ViewBag.BreadCrumbs = breadcrumbs;
            ViewBag.Title = windowTitle.ToString();
            ViewBag.PageTitle = pageTitle;
            if (seoDescription != null)
                ViewBag.MetaDescription = seoDescription;
            if (seoKeywords != null)
                ViewBag.MetaKeywords = seoKeywords;
            if (trackingTitles.Length > 0)
            {
                var secureTitle = windowTitle.ToString();
                foreach (var pair in trackingTitles)
                {
                    if (!String.IsNullOrEmpty(pair.First))
                        secureTitle = secureTitle.Replace(pair.First, pair.Second);
                }
                ViewBag.TrackingTitle = secureTitle;
            }
        }

        /// <summary>
        /// Gets the entity type count.
        /// </summary>
        /// <param name="contentTypeCounts">The content type counts.</param>
        /// <param name="type">The type.</param>
        /// <returns></returns>
        protected int? GetEntityTypeCount(IEnumerable<RefinementItem> contentTypeCounts, EntityType type)
        {
            var typeName = type.ToString();
            var refinementItem = contentTypeCounts.FirstOrDefault(x => x.Name == typeName);

            if (refinementItem == null)
                return null;
            return (int)refinementItem.Count;
        }

        /// <summary>
        /// Called when an unhandled exception occurs in the action.
        /// </summary>
        /// <param name="filterContext">Information about the current request and action.</param>
        protected override void OnException(ExceptionContext filterContext)
        {
            var viewResult = filterContext.Result as ViewResultBase;
            if (viewResult != null)
            {
                SetCommonViewBagProperties(viewResult.ViewBag);
            }

            SetCommonViewModelProperties(viewResult);

            //
            // this exception will not show up where our global error handling can find it (Server.getLastError()) so we must log it here
            //
            if (filterContext.Exception != null)
                _logHelper.LogError(HttpContext, filterContext.Exception);

            base.OnException(filterContext);
        }


        private void SetCommonViewModelProperties(ViewResultBase view)
        {
            if (view == null) return;

            var model = view.Model as BaseViewModel;
            if (model != null)
            {
                model.UlApps = _portalConfiguration.UlApps;
                if (!model.PageLinks.Any())
                {
                    model.PageLinks = HomeController.BuildPageLinks(_userContext, Url);
                }
            }
        }

        /// <summary>
        /// Builds the large json result.
        /// </summary>
        /// <param name="data">The data.</param>
        /// <returns>JsonResult.</returns>
        protected static JsonResult BuildLargeJsonResult(object data)
        {
            return new JsonResult()
            {
                Data = data,
                ContentType = "application/json",
                ContentEncoding = Encoding.UTF8,
                JsonRequestBehavior = JsonRequestBehavior.AllowGet,
                MaxJsonLength = Int32.MaxValue
            };
        }

        /// <summary>
        /// Shoulds the redirect.
        /// </summary>
        /// <param name="useDefaultSearch">if set to <c>true</c> [use default search].</param>
        /// <param name="profileProvider">The profile provider.</param>
        /// <param name="pageOrderQueue">The page order queue.</param>
        /// <param name="redirect">The redirect.</param>
        /// <returns></returns>
        protected bool ShouldRedirect(bool useDefaultSearch, IProfileProvider profileProvider, string pageOrderQueue, out ActionResult redirect)
        {
            string controller = ControllerContext.RouteData.Values["controller"] as String;
            string action = ControllerContext.RouteData.Values["action"] as String;
            var location = @"/" + controller + @"/" + action;
            var favoriteSearch = profileProvider.FetchActiveByLocation(location, _userContext);
            if (useDefaultSearch && favoriteSearch != null)
            {
                var queryString = favoriteSearch.Criteria.ToQueryString();
                {
                    redirect = new RedirectResult(pageOrderQueue.Replace("useDefaultSearch=True", "") + queryString);
                    return true;
                }
            }
            redirect = null;
            return false;
        }



        /// <summary>
        /// Shoulds the redirect.
        /// </summary>
        /// <param name="useDefaultSearch">if set to <c>true</c> [use default search].</param>
        /// <param name="profileProvider">The profile provider.</param>
        /// <returns></returns>
        protected SearchCriteria GetDefaultCriteriaIfApplicable(bool useDefaultSearch, IProfileProvider profileProvider)
        {
            if (useDefaultSearch)
            {
                var favoriteSearch = profileProvider.FetchActiveByLocation(HttpContext.Request.Url.AbsolutePath, _userContext);
                if (favoriteSearch != null)
                {

                    var queryParamCollection = HttpUtility.ParseQueryString(HttpContext.Request.Url.Query);
                    queryParamCollection.Remove("useDefaultSearch");
                    var tempUri = new Uri(HttpContext.Request.Url.AbsoluteUri);

                    var criteriaParams = HttpUtility.ParseQueryString(favoriteSearch.Criteria.ToQueryString());
                    foreach (string key in queryParamCollection.Keys)
                    {
                        criteriaParams.Add(key, queryParamCollection[key]);
                    }

                    HttpContext.Response.Redirect(HttpContext.Request.Url.AbsolutePath + "?" + criteriaParams.ToQueryString(), true);
                    return favoriteSearch.Criteria;
                }
            }
            return null;
        }




    }
}