using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using Microsoft.AspNet.SignalR;
using UL.Aria.Common.Authorization;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Http;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.DocumentTemplate;
using UL.Aria.Web.Common.Models.Search;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Mvc.Attributes;
using UL.Aria.Web.Common.Providers;
using UL.Aria.Web.Common.SignalR;
using UL.Enterprise.Foundation.Logging;
using UL.Aria.Web.Common.Mvc;

namespace UL.Aria.Web.Portal.Controllers
{
	/// <summary>
	/// Provides a Controller for Document Templates
	/// </summary>
	
	public class DocumentTemplateController : TemplateAdminBaseController
	{
		private readonly IDocumentTemplateProvider _documentTemplateProvider;
		private readonly IBusinessUnitProvider _businessUnitProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentTemplateController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="documentTemplateProvider">The document template provider.</param>
        /// <param name="businessUnitProvider">The business unit provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
		public DocumentTemplateController(IUserContext userContext, ILogHelper logHelper, IPortalConfiguration portalConfiguration, 
            IDocumentTemplateProvider documentTemplateProvider, IBusinessUnitProvider businessUnitProvider,
            ITempDataProviderFactory tempDataProviderFactory)
			: base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
		{
			_documentTemplateProvider = documentTemplateProvider;
			_businessUnitProvider = businessUnitProvider;
		}

		/// <summary>
		/// Indexes this instance.
		/// </summary>
		/// <returns></returns>
		[AuthorizeClaim(Resource = SecuredResources.ProjectTemplateAdministration, Action = SecuredActions.View)]
		public ActionResult Index(SearchCriteria searchCriteria)
		{
			if (searchCriteria == null)
				searchCriteria = new SearchCriteria();
			searchCriteria.ApplyDocumentTemplateSearch();
			var model = _documentTemplateProvider.Search(searchCriteria, _userContext);
			model.PageLinks = ActionsLeft(EntityType.DocumentTemplate.ToString());
			model.Breadcrumbs = Breadcrumbs("Document Templates", true);
			model.PageActions = ActionsRight(EntityType.DocumentTemplate);
			return View("~/Views/TemplateAdmin/Search.cshtml", model);
		}

		/// <summary>
		/// Creates this instance.
		/// </summary>
		/// <returns></returns>
		[HttpGet]
		[AuthorizeClaim(Resource = SecuredResources.ProjectTemplateAdministration, Action = SecuredActions.View)]
		public ActionResult Create()
		{
			var model = new DocumentTemplate()
			{
				UploadId = Guid.NewGuid().ToString(),
				EnableLogging = _portalConfiguration.EnableUploadUiLogging,
				BusinessUnits = _businessUnitProvider.FetchAllAsSelectListItems().ToList()
			};
			return PartialView("_Create", model);

		}


		/// <summary>
		/// Creates the specified document template.
		/// </summary>
		/// <param name="documentTemplate">The document template.</param>
		/// <returns></returns>
		[HttpPost]
		[AuthorizeClaim(Resource = SecuredResources.ProjectTemplateAdministration, Action = SecuredActions.View)]
		public ActionResult Create(DocumentTemplate documentTemplate)
		{
			dynamic client = null;

			try
			{
				if (documentTemplate.UploadId != null)
				{
					IHubContext uploadHubContext = GlobalHost.ConnectionManager.GetHubContext<UploadHub>();
					client = uploadHubContext.Clients.Client(documentTemplate.UploadId);
					FileUploadWorkerProcess.ClientUpdateProgress(client, 90, "Saving file...");
				}

				ValidateTemplateCreate(documentTemplate);
				if (ModelState.IsValid)
				{
					_documentTemplateProvider.Create(documentTemplate);
					ViewBag.Success = true;
					var message = string.Format("Document Template <strong> {0} </strong> has been successfully created.",
						documentTemplate.Name);
					AddPageMessage(message, severity: TraceEventType.Information, title: "Success!");

					if (client != null)
					{
						FileUploadWorkerProcess.ClientUpdateProgress(client, 100, "Upload completed successfully");
						FileUploadWorkerProcess.ClientComplete(client, documentTemplate.OriginalFileName,
							"Upload completed successfully");
					}

				}
				//else
				//{
				//	if (client != null)
				//		FileUploadWorkerProcess.ClientError(client, "Upload failed: ");

				//}

			}
			catch (Exception exception)
			{
				if (client != null)
					FileUploadWorkerProcess.ClientError(client, "Upload failed: " + exception.GetBaseException().Message);

				ModelState.AddModelError("", exception.GetBaseException().Message);
				_logHelper.Log(Logging.MessageIds.DocumentTemplateException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext, "Error while creating document template", exception);
			}
			finally
			{
				if (documentTemplate != null && !string.IsNullOrEmpty(documentTemplate.TempFilePath))
					System.IO.File.Delete(documentTemplate.TempFilePath);

			}

			return PartialView("_Create", documentTemplate);
		}


		private void ValidateTemplateCreate(DocumentTemplate documentTemplate)
		{
			if (documentTemplate != null && System.IO.File.Exists(documentTemplate.TempFilePath))
			{
				ModelState.Remove("File");
			}

			if (!documentTemplate.BusinessUnits.Any(x => x.Selected))
			{
				ModelState.AddModelError("BusinessUnits", "BusinessUnits is required.");
			}

		}

		/// <summary>
		/// Edits the specified identifier.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <returns></returns>
		[HttpGet]
		[AuthorizeClaim(Resource = SecuredResources.ProjectTemplateAdministration, Action = SecuredActions.View)]
		public ActionResult Edit(Guid id)
		{
			var model = _documentTemplateProvider.FetchByIdForEdit(id);
			model.UploadId = Guid.NewGuid().ToString();
			return PartialView("_Edit", model);
		}

		/// <summary>
		/// Edits the specified document template.
		/// </summary>
		/// <param name="documentTemplate">The document template.</param>
		/// <returns></returns>
		[HttpPost]
		[AuthorizeClaim(Resource = SecuredResources.ProjectTemplateAdministration, Action = SecuredActions.View)]
		public ActionResult Edit(DocumentTemplateEdit documentTemplate)
		{
			dynamic client = null;
			if (documentTemplate.UploadId != null)
			{
				IHubContext uploadHubContext = GlobalHost.ConnectionManager.GetHubContext<UploadHub>();
				client = uploadHubContext.Clients.Client(documentTemplate.UploadId);
				FileUploadWorkerProcess.ClientUpdateProgress(client, 90, "Saving file...");
			}
			try
			{
				if (!documentTemplate.BusinessUnits.Any(x => x.Selected))
				{
					ModelState.AddModelError("BusinessUnits", "BusinessUnits is required.");
				}


				if (ModelState.IsValid)
				{
					_documentTemplateProvider.Update(documentTemplate);
					ViewBag.Success = true;
					var message = string.Format("Document Template <strong> {0} </strong> has been successfully updated.", documentTemplate.Name);
					AddPageMessage(message, severity: TraceEventType.Information, title: "Success!");
					if (client != null)
					{
						FileUploadWorkerProcess.ClientUpdateProgress(client, 100, "Upload completed successfully");
						FileUploadWorkerProcess.ClientComplete(client, documentTemplate.OriginalFileName,
							"Upload completed successfully");
					}
				}
			}
			catch (Exception exception)
			{
				if (client != null)
					FileUploadWorkerProcess.ClientError(client, "Upload failed: " + exception.GetBaseException().Message);

				ModelState.AddModelError("", exception.GetBaseException().Message);
				_logHelper.Log(Logging.MessageIds.DocumentTemplateException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext, "Error while updating document template", exception);
			}
			finally
			{
				if (documentTemplate != null && !string.IsNullOrEmpty(documentTemplate.TempFilePath))
					System.IO.File.Delete(documentTemplate.TempFilePath);

			}


			return PartialView("_Edit", documentTemplate);

		}


		/// <summary>
		/// Deletes the specified identifier.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <returns></returns>
		[AuthorizeClaim(Resource = SecuredResources.ProjectTemplateAdministration, Action = SecuredActions.View)]
		public JsonResult Delete(Guid id)
		{
			GrowlMessage message = null;
			bool success = false;
			try
			{
				_documentTemplateProvider.Delete(id);
				success = true;
				message = CreatePageMessage("Document Template have been deleted successfully.", title: "Success!");
				AddPageMessage(message);

			}
			catch (Exception exception)
			{
				var ex = exception.GetBaseException() as WebException;
				var canDeleteDocumentTemplate = ex != null && (ex.Response as HttpWebResponse) != null && (ex.Response as HttpWebResponse).StatusCode == HttpStatusCode.MethodNotAllowed;
				message = canDeleteDocumentTemplate
					? CreatePageMessage("Document Template is in use, can't delete it.", title: "Error", severity: TraceEventType.Error,
						sticky: true)
					: CreatePageMessage("There was an error deleting Document Template." + exception.GetBaseException().Message,
						title: "Error", severity: TraceEventType.Error, sticky: true);
				_logHelper.Log(Logging.MessageIds.DocumentTemplateException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext, "Error while Deleting Document Template", exception);

				AddPageMessage(message);
			}

			return Json(new
			{
				success,
				message
			});
		}


		/// <summary>
		///     Downloads the specified id.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <returns></returns>
		[ULEmployee]
		public FileStreamResult Download(Guid id)
		{
			ContentDownload fileData = _documentTemplateProvider.DownloadById(id);

			return File(fileData.OutputStream, fileData.ContentType, fileData.FileName);
		}

		/// <summary>
		/// Validates the document template.
		/// </summary>
		/// <param name="documentTemplate">The document template.</param>
		/// <returns></returns>
		[HttpPost]
		[AuthorizeClaim(Resource = SecuredResources.ProjectTemplateAdministration, Action = SecuredActions.View)]
		public JsonResult ValidateDocumentTemplate(DocumentTemplate documentTemplate)
		{

			var errors = new List<ValidationError>();

			if (documentTemplate == null || string.IsNullOrEmpty(documentTemplate.Name))
			{
				errors.Add(new ValidationError() { Key = "Name", ErrorMessage = "Name is Required." });
			}

			if (documentTemplate == null || !documentTemplate.BusinessUnits.Any(x => x.Selected))
			{
				errors.Add(new ValidationError() { Key = DocumentTemplate.BusinessUnitValidationKey, ErrorMessage = "BusinessUnits is Required." });
			}

            var otherErrors = this._documentTemplateProvider.Validate(documentTemplate);
            if (otherErrors != null && otherErrors.Count() > 0)
            {              
                errors.AddRange(otherErrors);
            }


			var response = new JsonResponseModel() { Successful = true };
			if (errors.Any())
			{
				response.Successful = false;
				response.Data = errors;
			}
			return Json(response);
		}



	}
}
