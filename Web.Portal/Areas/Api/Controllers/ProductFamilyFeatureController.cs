using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UL.Aria.Common;
using UL.Enterprise.Foundation;
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
    /// Controller for <see cref="ProductFamilyFeature"/> operations
    /// </summary>
    [AuthorizeProductAdmin]
    public class ProductFamilyFeatureController : BaseController
    {
        private readonly IProductFamilyFeatureProvider _productFamilyFeatureProvider;
        //
        // GET: /Api/ProductFamilyFeature/


        /// <summary>
        /// Initializes a new instance of the <see cref="BaseController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="productFamilyFeatureProvider">The product family Feature prof provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
        public ProductFamilyFeatureController(IUserContext userContext, ILogHelper logHelper, IPortalConfiguration portalConfiguration
            , IProductFamilyFeatureProvider productFamilyFeatureProvider
            , ITempDataProviderFactory tempDataProviderFactory
            ) 
            : base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
        {
            _productFamilyFeatureProvider = productFamilyFeatureProvider;
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
        /// Fetches all.
        /// </summary>
        /// <param name="id">The unique identifier.</param>
        /// <param name="scopeIds">The scope ids.</param>
        /// <returns>
        /// JsonResult.
        /// </returns>
        /// <exception cref="System.Web.HttpException">400;Invalid entity id.</exception>
        [HttpGet]
        [ActionName("Crud")]
        public JsonResult Fetch(string id, string scopeIds)
        {
            if (!string.IsNullOrEmpty(id))
            {
                Guid parsedId;
                if (Guid.TryParse(id, out parsedId))
                    return Fetch(parsedId);

                throw new HttpException(400, "Invalid entity id.");
            }
            if (!string.IsNullOrEmpty(scopeIds))
            {
                
                var splitScopeIds = scopeIds.Split(',');
                var convertedIds = new List<Guid>(splitScopeIds.Length);
                foreach (var scopeid in splitScopeIds)
                {
                    var convertedId = scopeid.ParseOrDefault(Guid.Empty);
                    if (convertedId == Guid.Empty)
                    {
                        throw new HttpException(400, "Invalid scope id."); 
                    }
                    convertedIds.Add(convertedId);
                }
                return Fetch(convertedIds);
            }

            var entities = _productFamilyFeatureProvider.Fetch();
            return BuildLargeJsonResult(entities);
        }

        /// <summary>
        ///     Fetches the specified category vy id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>JsonResult.</returns>
        [HttpGet]
        public JsonResult Fetch(Guid id)
        {
            var entity = _productFamilyFeatureProvider.Fetch(id);
            return BuildLargeJsonResult(entity);
        }

        /// <summary>
        /// Fetches the scope.
        /// </summary>
        /// <param name="scopeIds">The scope ids.</param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult Fetch(IEnumerable<Guid> scopeIds)
        {
            var entities = _productFamilyFeatureProvider.FetchByScope(scopeIds);
            return BuildLargeJsonResult(entities);
        }

        /// <summary>
        /// Creates the specified category.
        /// </summary>
        /// <param name="productFamilyFeature">The category.</param>
        /// <returns>
        /// JsonResult.
        /// </returns>
        [HttpPost]
        [ActionName("Crud")]
        public JsonResult Create(ProductFamilyFeature productFamilyFeature)
        {
            var id = _productFamilyFeatureProvider.Create(productFamilyFeature, _userContext);
            return Json(id.ToString());
        }

        /// <summary>
        /// Updates the specified category by  id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="productFamilyFeature">The product family Feature.</param>
        /// <returns>
        /// JsonResult.
        /// </returns>
        [HttpPut]
        [ActionName("Crud")]
        public EmptyResult Update(Guid id, ProductFamilyFeature productFamilyFeature)
        {
            if (productFamilyFeature.Id.HasValue && productFamilyFeature.Id.Value != id)
                throw new HttpException(409, "If Id is provided in content, it must match the id on the URI");
            _productFamilyFeatureProvider.Update(productFamilyFeature, _userContext);
            return new EmptyResult();
        }

        /// <summary>
        /// Deletes the specified category by id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>
        /// EmptyResult.
        /// </returns>
        [HttpDelete]
        [ActionName("Crud")]
        public EmptyResult Delete(Guid id)
        {
            _productFamilyFeatureProvider.Remove(id);
            return new EmptyResult();
        }

    }
}
