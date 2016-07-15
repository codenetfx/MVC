using System.Diagnostics.CodeAnalysis;
using System.Web.Mvc;
using System.Web.Http;

namespace UL.Aria.Web.Portal.Areas.Api
{
    /// <summary>
    /// API Area Registration
    /// </summary>
    [ExcludeFromCodeCoverage] // Route assignments
    public class ApiAreaRegistration : AreaRegistration
    {
        /// <summary>
        /// Gets the name of the area to register.
        /// </summary>
        /// <returns>The name of the area to register.</returns>
        public override string AreaName
        {
            get
            {
                return "api";
            }
        }

        /// <summary>
        /// Registers an area in an ASP.NET MVC application using the specified area's context information.
        /// </summary>
        /// <param name="context">Encapsulates the information that is required in order to register the area.</param>
        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute("Api_Crud", "api/v1/{controller}/{id}", new { action = "Crud", id = UrlParameter.Optional });

            context.MapRoute(
                "Api_default",
                "api/{controller}/{action}/{id}",
                new { action = "Index", id = UrlParameter.Optional }
            );
        }
    }
}
