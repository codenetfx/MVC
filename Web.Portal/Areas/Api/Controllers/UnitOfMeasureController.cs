using System;
using System.Web;
using System.Web.Mvc;
using AutoMapper;
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
    ///     Class UnitOfMeasureController
    /// </summary>
    [AuthorizeProductAdmin]
    public class UnitOfMeasureController : BaseController
    {
        private readonly IUnitOfMeasureProvider _unitOfMeasureProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfMeasureController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="unitOfMeasureProvider">The unit of measure provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
        public UnitOfMeasureController(IUserContext userContext, ILogHelper logHelper,
            IPortalConfiguration portalConfiguration, IUnitOfMeasureProvider unitOfMeasureProvider, ITempDataProviderFactory tempDataProviderFactory)
            : base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
        {
            _unitOfMeasureProvider = unitOfMeasureProvider;
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
        /// <param name="id"></param>
        /// <returns>JsonResult.</returns>
        [HttpGet]
        [ActionName("Crud")]
        public JsonResult Fetch(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                Guid parsedId;
                if (Guid.TryParse(id, out parsedId))
                {
                    return Fetch(parsedId);
                }

                throw new HttpException(400, "Invalid entity id.");
            }

            var entities = _unitOfMeasureProvider.FetchAll();
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
            var entity = _unitOfMeasureProvider.Fetch(id);
            return BuildLargeJsonResult(entity);
        }

        /// <summary>
        ///     Creates the specified unit of measure.
        /// </summary>
        /// <param name="unitOfMeasure">The unit of measure.</param>
        /// <returns>JsonResult.</returns>
        [HttpPost]
        [ActionName("Crud")]
        public JsonResult Create(UnitOfMeasure unitOfMeasure)
        {
            _unitOfMeasureProvider.Create(unitOfMeasure, _userContext);
            return Json(unitOfMeasure.Id.ToNullSafeString());
        }

        /// <summary>
        ///     Updates the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="unitOfMeasure">The unit of measure.</param>
        /// <returns>JsonResult.</returns>
        [HttpPut]
        [ActionName("Crud")]
        public EmptyResult Update(Guid id, UnitOfMeasure unitOfMeasure)
        {
            _unitOfMeasureProvider.Update(id, unitOfMeasure, _userContext);
            return new EmptyResult();
        }

        /// <summary>
        /// Deletes the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="justHereForCompilerUniqueness">Just here for compiler uniqueness.</param>
        /// <returns>
        /// JsonResult.
        /// </returns>
        [HttpDelete]
        [ActionName("Crud")]
        public EmptyResult Delete(Guid id, string justHereForCompilerUniqueness)
        {
            _unitOfMeasureProvider.Delete(id);
            return new EmptyResult();
        }
    }
}