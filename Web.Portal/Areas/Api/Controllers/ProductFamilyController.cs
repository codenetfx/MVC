using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web;
using System.Web.Mvc;
using UL.Aria.Common;
using UL.Enterprise.Foundation.Logging;
using UL.Enterprise.Foundation.Mapper;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Product;
using UL.Aria.Web.Common.Mvc.Attributes;
using UL.Aria.Web.Common.Providers;
using UL.Aria.Web.Portal.Controllers;
using UL.Aria.Web.Common.Mvc;

namespace UL.Aria.Web.Portal.Areas.Api.Controllers
{
    /// <summary>
    ///     Class ProductFamilyController
    /// </summary>
    [AuthorizeProductAdmin]
    public class ProductFamilyController : BaseController
    {
        private readonly IProductFamilyProvider _productFamilyProvider;
        private readonly IMapperRegistry _mapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="productFamilyProvider">The product family provider.</param>
        /// <param name="mapper">The mapper.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
        public ProductFamilyController(IUserContext userContext, ILogHelper logHelper, IPortalConfiguration portalConfiguration,
            IProductFamilyProvider productFamilyProvider, IMapperRegistry mapper, ITempDataProviderFactory tempDataProviderFactory)
            : base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
        {
            _productFamilyProvider = productFamilyProvider;
            _mapper = mapper;
        }

        /// <summary>
        ///     Gets the logging category to use for all logging.
        /// </summary>
        /// <value>The logging category.</value>
        protected override LogCategory LoggingCategory
        {
            get { return LogCategory.ProductFamily; }
        }

        /// <summary>
        ///     Fetches the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>JsonResult.</returns>
        /// <exception cref="System.Web.HttpException">
        ///     400;Invalid entity id.
        ///     or
        ///     400;Invalid entity id.
        /// </exception>
        [HttpGet]
        [ActionName("Crud")]
        public JsonResult Fetch(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                Guid parsedId;
                if (Guid.TryParse(id, out parsedId))
                    return Fetch(parsedId);

                throw new HttpException(400, "Invalid entity id.");
            }

            var entities = _mapper.Map<IList<ProductFamilyEx>>(_productFamilyProvider.FetchAll());
            return BuildLargeJsonResult(entities);
        }

        /// <summary>
        ///     Fetches the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>JsonResult.</returns>
        [HttpGet]
        public JsonResult Fetch(Guid id)
        {
            var entity = _mapper.Map<ProductFamilyEx>(_productFamilyProvider.FetchDetail(id));
            return BuildLargeJsonResult(entity);
        }

        /// <summary>
        /// Creates the specified product family.
        /// </summary>
        /// <param name="productFamily">The product family.</param>
        /// <returns>EmptyResult.</returns>
        [HttpPost]
        [ActionName("Crud")]
        public JsonResult Create(ProductFamilyEx productFamily)
        {
            var familyDetail = _mapper.Map<ProductFamilyDetail>(productFamily);
            _productFamilyProvider.Create(familyDetail.ProductFamily,
                familyDetail.Characteristics, familyDetail.Dependencies);
            return Json(productFamily.Id.ToString());
        }

        /// <summary>
        /// Updates the specified product family.
        /// </summary>
        /// <param name="productFamily">The product family.</param>
        /// <returns>EmptyResult.</returns>
        [HttpPut]
        [ActionName("Crud")]
        public EmptyResult Update(ProductFamilyEx productFamily)
        {
            var familyDetail = _mapper.Map<ProductFamilyDetail>(productFamily);
            _productFamilyProvider.Update(familyDetail.ProductFamily,
                familyDetail.Characteristics, familyDetail.Dependencies);
            return new EmptyResult();
        }
    }
}