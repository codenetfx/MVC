using System;
using System.Web.Mvc;
using UL.Enterprise.Foundation.Logging;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Product;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Mvc.Attributes;
using UL.Aria.Web.Common.Providers;
using UL.Aria.Web.Common.Mvc;

namespace UL.Aria.Web.Portal.Controllers
{
    /// <summary>
    /// </summary>
    [Authorize]
    public class ProductFamilyController : BaseController
    {
        private readonly IProductFamilyProvider _productFamilyProvider;
        //
        // GET: /ProductFamily/

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="productFamilyProvider">The product family provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
        public ProductFamilyController(
            IUserContext userContext,
            ILogHelper logHelper,
            IPortalConfiguration portalConfiguration,
            IProductFamilyProvider productFamilyProvider,
            ITempDataProviderFactory tempDataProviderFactory)
            : base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
        {
            _productFamilyProvider = productFamilyProvider;
        }

        /// <summary>
        ///     Gets the logging category to use for all logging.
        /// </summary>
        /// <value>
        ///     The logging category.
        /// </value>
        protected override LogCategory LoggingCategory
        {
            get { return LogCategory.ProductFamily; }
        }

        /// <summary>
        ///     Fetches the specified Product Family.
        /// </summary>
        /// <param name="id">The unique identifier.</param>
        /// <returns></returns>
        [AuthorizeProductAdmin]
        public JsonResult Fetch(string id)
        {
            var guid = new Guid(id);
            ProductFamily productFamily = _productFamilyProvider.Fetch(guid);
            return Json(productFamily, JsonRequestBehavior.AllowGet);
        }


        /// <summary>
        ///     Downloads the product family template.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="template">The template.</param>
        /// <returns></returns>
        public FileStreamResult DownloadProductFamilyTemplate(Guid id, string template)
        {
            ContentDownload fileData = _productFamilyProvider.Fetch(id, template);
            return File(fileData.OutputStream, fileData.ContentType, fileData.FileName);
        }
    }
}