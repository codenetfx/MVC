using System;
using System.Net;
using System.Web;
using System.Web.Mvc;
using Microsoft.Ajax.Utilities;
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
    ///     Class CategoryController
    /// </summary>
    [AuthorizeProductAdmin]
    public class CategoryController : BaseController
    {
        private readonly ICategoryProvider _categoryProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="categoryProvider">The category provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
        public CategoryController(IUserContext userContext, ILogHelper logHelper,
            IPortalConfiguration portalConfiguration, ICategoryProvider categoryProvider, ITempDataProviderFactory tempDataProviderFactory)
            : base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
        {
            _categoryProvider = categoryProvider;
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
        /// <param name="id"></param>
        /// <returns>
        /// JsonResult.
        /// </returns>
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

            var entities = _categoryProvider.FetchAll();
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
            var entity = _categoryProvider.Fetch(id);
            return BuildLargeJsonResult(entity);
        }

        /// <summary>
        ///     Creates the specified category.
        /// </summary>
        /// <param name="category">The category.</param>
        /// <returns>JsonResult.</returns>
        [HttpPost]
        [ActionName("Crud")]
        public JsonResult Create(Category category)
        {
            var id = _categoryProvider.Create(category, _userContext);
            return Json(id.ToString());
        }

        /// <summary>
        ///     Updates the specified category by  id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="category">The category.</param>
        /// <returns>JsonResult.</returns>
        [HttpPut]
        [ActionName("Crud")]
        public EmptyResult Update(Guid id, Category category)
        {
            _categoryProvider.Update(id, category, _userContext);
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
            _categoryProvider.Delete(id);
            return new EmptyResult();
        }
    }
}