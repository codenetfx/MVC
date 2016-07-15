using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Profile;
using UL.Aria.Web.Common.Models.Search;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Providers;
using UL.Enterprise.Foundation.Logging;

namespace UL.Aria.Web.Portal.Controllers
{
    /// <summary>
    /// Controller for reference items
    /// </summary>
    [Authorize]
    public class ReferenceController : BaseController
    {
        private readonly IIndustryCodeProvider _industryCodeProvider;
        private readonly IServiceCodeProvider _serviceCodeProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ReferenceController" /> class.
        /// </summary>
        /// <param name="industryCodeProvider">The industry code provider.</param>
        /// <param name="serviceCodeProvider">The service code provider.</param>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
        public ReferenceController(IIndustryCodeProvider industryCodeProvider, IServiceCodeProvider serviceCodeProvider,
            IUserContext userContext, ILogHelper logHelper, IPortalConfiguration portalConfiguration,
            ITempDataProviderFactory tempDataProviderFactory)
            :base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
        {
            _industryCodeProvider = industryCodeProvider;
            _serviceCodeProvider = serviceCodeProvider;
        }

        /// <summary>
        /// Searches the service codes.
        /// </summary>
        /// <param name="keyword">The keyword.</param>
        /// <returns></returns>
        public ActionResult ServiceCode(string keyword)
        {
            var results = _serviceCodeProvider.Search(
                new SearchCriteria {Query = keyword, Paging = new Paging{PageSize = 100}},
                this._userContext
                ); 

            return Json(new JsonResponseModel()
            {
                Successful = true,
                Data = results.Results.Select(x => new
                {
                  Id = x.ExternalId,
				  Description = x.ExternalId,
				  Display = string.Format("{0} {1}", x.ExternalId, x.Label)
                }),
                ErrorCode = 200
            });
        }

        /// <summary>
        /// Searches the industry codes.
        /// </summary>
        /// <param name="keyword">The keyword.</param>
        /// <returns></returns>
        public ActionResult IndustryCode(string keyword)
        {
            var results = _industryCodeProvider.Search(
                new SearchCriteria { Query = keyword, Paging = new Paging { PageSize = 100 } },
                this._userContext
                );

            return Json(new JsonResponseModel()
            {
                Successful = true,
                Data = results.Results.Select(x => new
                {
                    Id = x.ExternalId,
					Description = x.ExternalId,
					Display = string.Format("{0} {1}", x.ExternalId , x.Label)
                }),
                ErrorCode = 200
            });
        }

        /// <summary>
        /// Gets the logging category to use for all logging.
        /// </summary>
        /// <value>
        /// The logging category.
        /// </value>
        protected override LogCategory LoggingCategory
        {
            get { return LogCategory.System; }
        }
    }
}
