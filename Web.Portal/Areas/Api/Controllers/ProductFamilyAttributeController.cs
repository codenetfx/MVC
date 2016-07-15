using System;
using System.Collections.Generic;
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
    ///     Controller for <see cref="ProductFamilyAttribute" /> operations
    /// </summary>
    [AuthorizeProductAdmin]
    public class ProductFamilyAttributeController : BaseController
    {
        private readonly IProductFamilyAttributeProvider _productFamilyAttributeProvider;
        //
        // GET: /Api/ProductFamilyAttribute/


        /// <summary>
        /// Initializes a new instance of the <see cref="BaseController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="productFamilyAttributeProvider">The product family attribute prof provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
        public ProductFamilyAttributeController(IUserContext userContext, ILogHelper logHelper,
            IPortalConfiguration portalConfiguration
            , IProductFamilyAttributeProvider productFamilyAttributeProvider
            , ITempDataProviderFactory tempDataProviderFactory
            )
            : base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
        {
            _productFamilyAttributeProvider = productFamilyAttributeProvider;
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
        /// <param name="id">The unique identifier.</param>
        /// <param name="scopeIds">The scope ids.</param>
        /// <returns>
        ///     JsonResult.
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
                string[] splitScopeIds = scopeIds.Split(',');
                var convertedIds = new List<Guid>(splitScopeIds.Length);
                foreach (string scopeid in splitScopeIds)
                {
                    Guid convertedId = scopeid.ParseOrDefault(Guid.Empty);
                    if (convertedId == Guid.Empty)
                    {
                        throw new HttpException(400, "Invalid scope id.");
                    }
                    convertedIds.Add(convertedId);
                }
                return Fetch(convertedIds);
            }

            IList<ProductFamilyAttribute> entities = _productFamilyAttributeProvider.Fetch();
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
            ProductFamilyAttribute entity = _productFamilyAttributeProvider.Fetch(id);
            return BuildLargeJsonResult(entity);
        }

        /// <summary>
        ///     Fetches the scope.
        /// </summary>
        /// <param name="scopeIds">The scope ids.</param>
        /// <returns></returns>
        [HttpGet]
        public JsonResult Fetch(IEnumerable<Guid> scopeIds)
        {
            IList<ProductFamilyAttribute> entity = _productFamilyAttributeProvider.FetchByScope(scopeIds);
            return BuildLargeJsonResult(entity);
        }

        /// <summary>
        ///     Creates the specified category.
        /// </summary>
        /// <param name="productFamilyAttribute">The category.</param>
        /// <returns>
        ///     JsonResult.
        /// </returns>
        [HttpPost]
        [ActionName("Crud")]
        public JsonResult Create(ProductFamilyAttribute productFamilyAttribute)
        {
            Guid id = _productFamilyAttributeProvider.Create(productFamilyAttribute, _userContext);
            return Json(id.ToString());
        }

        /// <summary>
        ///     Updates the specified category by  id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="productFamilyAttribute">The product family attribute.</param>
        /// <returns>
        ///     JsonResult.
        /// </returns>
        [HttpPut]
        [ActionName("Crud")]
        public EmptyResult Update(Guid id, ProductFamilyAttribute productFamilyAttribute)
        {
            if (productFamilyAttribute.Id.HasValue && productFamilyAttribute.Id.Value != id)
                throw new HttpException(409, "If Id is provided in content, it must match the id on the URI");
            _productFamilyAttributeProvider.Update(productFamilyAttribute, _userContext);
            return new EmptyResult();
        }

        /// <summary>
        ///     Deletes the specified category by id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns>
        ///     EmptyResult.
        /// </returns>
        [HttpDelete]
        [ActionName("Crud")]
        public EmptyResult Delete(Guid id)
        {
            _productFamilyAttributeProvider.Remove(id);
            return new EmptyResult();
        }
    }
}