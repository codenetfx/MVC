using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UL.Enterprise.Foundation.Logging;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Mvc.Attributes;
using UL.Aria.Web.Common.Providers;
using UL.Aria.Web.Portal.Controllers;
using UL.Aria.Web.Common.Mvc;

namespace UL.Aria.Web.Portal.Areas.Api.Controllers
{
    /// <summary>
    /// ProductController
    /// </summary>
    [AuthorizeProductAdmin]
    public class ProductController : BaseController
    {
        private readonly IProductProvider _productProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="productProvider">The product provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
        public ProductController(IUserContext userContext, ILogHelper logHelper, IPortalConfiguration portalConfiguration,
            IProductProvider productProvider, ITempDataProviderFactory tempDataProviderFactory)
            :base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
        {
            _productProvider = productProvider;
        }

        /// <summary>
        ///     Fetches the specified unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier.</param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult Fetch(Guid id)
        {
            var entity = _productProvider.FetchById(id, _userContext);
            if (null == entity)
                throw new HttpException(404, "Not Found");
            return BuildLargeJsonResult(entity);
        }

        /// <summary>
        ///     Fetches the specified unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier.</param>
        /// <returns></returns>
        /// <exception cref="System.Web.HttpException">400;Invalid entity id.</exception>
        [HttpGet]
        [ActionName("Crud")]
        public JsonResult Fetch(string id)
        {
            Guid parsedId;
            if (string.IsNullOrEmpty(id) || !Guid.TryParse(id, out parsedId))
            {
                throw new HttpException(400, "Invalid entity id.");
            }

            return Fetch(parsedId);
        }

        /// <summary>
        /// Gets the logging category to use for all logging.
        /// </summary>
        /// <value>
        /// The logging category.
        /// </value>
        protected override LogCategory LoggingCategory
        {
            get { return LogCategory.Product; }
        }
    }
}
