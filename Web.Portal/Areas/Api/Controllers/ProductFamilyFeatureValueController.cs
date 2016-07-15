using System;
using System.Collections.Generic;
using System.Web;
using System.Web.Mvc;
using UL.Enterprise.Foundation.Logging;
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
    ///     Class ProductFamilyFeatureValueController
    /// </summary>
    [AuthorizeProductAdmin]
    public class ProductFamilyFeatureValueController : BaseController
    {
        private readonly IProductFamilyFeatureValueProvider _productFamilyFeatureValueProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductFamilyFeatureValueController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="productFamilyFeatureValueProvider">The product family feature value provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
        public ProductFamilyFeatureValueController(IUserContext userContext, ILogHelper logHelper,
            IPortalConfiguration portalConfiguration,
            IProductFamilyFeatureValueProvider productFamilyFeatureValueProvider, ITempDataProviderFactory tempDataProviderFactory)
            : base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
        {
            _productFamilyFeatureValueProvider = productFamilyFeatureValueProvider;
        }

        /// <summary>
        ///     Gets the logging category to use for all logging.
        /// </summary>
        /// <value>The logging category.</value>
        protected override LogCategory LoggingCategory
        {
            get { return LogCategory.Product; }
        }

        /// <summary>
        ///     Fetches all.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="productFeatureId">The product feature id.</param>
        /// <returns>JsonResult.</returns>
        /// <exception cref="System.Web.HttpException">
        ///     400;Invalid entity id.
        ///     or
        ///     400;Invalid family id.
        /// </exception>
        [HttpGet]
        [ActionName("Crud")]
        public JsonResult Fetch(Guid? id, Guid? productFeatureId)
        {
            if (!ModelState.IsValid)
                throw new HttpException(400, "Invalid entity id.");

            if (id != null)
            {
                return Fetch(id.Value);
            }

            if (productFeatureId != null)
            {
                IList<ProductFamilyFeatureValue> familyEntities =
                    _productFamilyFeatureValueProvider.FetchByProductFeatureId(productFeatureId.Value);
                return BuildLargeJsonResult(familyEntities);
            }

            IList<ProductFamilyFeatureValue> entities = _productFamilyFeatureValueProvider.FetchAll();
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
            ProductFamilyFeatureValue entity = _productFamilyFeatureValueProvider.Fetch(id);
            return Json(entity, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        ///     Creates the specified product family feature value.
        /// </summary>
        /// <param name="productFamilyFeatureValue">The product family feature value.</param>
        /// <returns>JsonResult.</returns>
        [HttpPost]
        [ActionName("Crud")]
        public JsonResult Create(ProductFamilyFeatureValue productFamilyFeatureValue)
        {
            Guid id = _productFamilyFeatureValueProvider.Create(productFamilyFeatureValue, _userContext);
            return Json(id.ToString());
        }

        /// <summary>
        ///     Updates the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="productFamilyFeatureValue">The product family feature value.</param>
        /// <returns>EmptyResult.</returns>
        [HttpPut]
        [ActionName("Crud")]
        public EmptyResult Update(Guid id, ProductFamilyFeatureValue productFamilyFeatureValue)
        {
            _productFamilyFeatureValueProvider.Update(id, productFamilyFeatureValue, _userContext);
            return new EmptyResult();
        }

        /// <summary>
        ///     Deletes the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>EmptyResult.</returns>
        [HttpDelete]
        [ActionName("Crud")]
        public EmptyResult Delete(Guid id)
        {
            _productFamilyFeatureValueProvider.Delete(id);
            return new EmptyResult();
        }
    }
}