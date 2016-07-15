using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Web.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace UL.Aria.Web.Portal.Controllers
{
    /// <summary>
    /// result
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class JsonNetResult : JsonResult
    {
        /// <summary>
        /// Enables processing of the result of an action method by a custom type that inherits from the <see cref="T:System.Web.Mvc.ActionResult" /> class.
        /// </summary>
        /// <param name="context">The context within which the result is executed.</param>
        /// <exception cref="System.ArgumentNullException">context</exception>
        public override void ExecuteResult(ControllerContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            var response = context.HttpContext.Response;

            response.ContentType = !String.IsNullOrEmpty(ContentType)
                ? ContentType
                : "application/json";

            if (ContentEncoding != null)
                response.ContentEncoding = ContentEncoding;
            var defaulJsonSettings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Objects,
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                DateFormatHandling = DateFormatHandling.MicrosoftDateFormat
                //Converters = new List<JsonConverter> { new ExpandoObjectConverter() }
            };
            // If you need special handling, you can call another form of SerializeObject below
            var serializedObject = JsonConvert.SerializeObject(Data, JsonSettings ?? defaulJsonSettings);
            response.Write(serializedObject);
        }


        /// <summary>
        /// Gets or sets the json settings.
        /// </summary>
        /// <value>
        /// The json settings.
        /// </value>
        public JsonSerializerSettings JsonSettings { get; set; }
    }
}