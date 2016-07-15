using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Web;
using System.Web.Mvc;
using Microsoft.Ajax.Utilities;
using Microsoft.AspNet.SignalR;
using UL.Aria.Web.Portal.Logging;
using UL.Enterprise.Foundation;
using UL.Enterprise.Foundation.Logging;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Http;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Search;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Providers;
using UL.Aria.Web.Common.SignalR;
using UL.Aria.Web.Common.Models.Container;
using UL.Aria.Web.Common.Mvc;

namespace UL.Aria.Web.Portal.Controllers
{
	/// <summary>
	/// </summary>
	[System.Web.Mvc.Authorize]
	public class ContainerController : BaseController
	{
		private readonly IAriaProvider _ariaProvider;
		private readonly IContainerProvider _containerProvider;
		private readonly IFileTransferProvider _fileTransferProvider;
		private readonly ISearchProvider _searchProvider;
		private readonly ISessionProvider _sessionProvider;
		private readonly IDocumentProvider _documentProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompanyController" /> class.
        /// </summary>
        /// <param name="ariaProvider">The aria provider.</param>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="fileTransferProvider">The file transfer provider.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="searchProvider">The search provider.</param>
        /// <param name="containerProvider">The container provider.</param>
        /// <param name="sessionProvider">The session provider.</param>
        /// <param name="documentProvider">The document provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
		public ContainerController(IAriaProvider ariaProvider, IUserContext userContext,
			ILogHelper logHelper, IFileTransferProvider fileTransferProvider,
			IPortalConfiguration portalConfiguration, ISearchProvider searchProvider,
            IContainerProvider containerProvider, ISessionProvider sessionProvider, IDocumentProvider documentProvider, 
            ITempDataProviderFactory tempDataProviderFactory)
			: base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
		{
			_ariaProvider = ariaProvider;
			_fileTransferProvider = fileTransferProvider;
			_searchProvider = searchProvider;
			_containerProvider = containerProvider;
			_sessionProvider = sessionProvider;
			_documentProvider = documentProvider;
		}

		/// <summary>
		///     Gets the logging category to use for all logging.
		/// </summary>
		/// <value>
		///     The logging category.
		/// </value>
		protected override LogCategory LoggingCategory
		{
			get { return LogCategory.Container; }
		}

		/// <summary>
		///     Uploads this instance.
		/// </summary>
		/// <param name="id">The asset id.</param>
		/// <param name="entityType">the entity type</param>
		/// <param name="containerId">The container id.  If not provided, the system will look it up via a search.</param>
		/// <returns></returns>
		/// <exception cref="System.Web.HttpException">Container not found</exception>
		public ActionResult Upload(Guid id, EntityType entityType, Guid? containerId)
		{
			if (containerId == null || containerId.Value == Guid.Empty)
				containerId = _searchProvider.GetContainerId(id, _sessionProvider, _userContext);

			if (containerId == null)
				throw new HttpException((int)HttpStatusCode.NotFound, "The requested container could not be found");

			var model = new DocumentUpload
			{
				EnableLogging = _portalConfiguration.EnableUploadUiLogging,
				ContainerId = containerId.Value,
				DocumentTypes = DocumentMetadataUpload.AuthorizedDocumentListItems(_portalConfiguration.DocumentTypes),
				UploadId = Guid.NewGuid().ToString("N"),
				LastModifiedBy = _userContext.LoginId
			};

			if (_userContext.CanSpecifyDocumentPermissions(containerId.Value))
			{
				model.Permissions = GetDocumentPermissions(containerId.Value);
				model.Permission = DocumentPermission.Private;
			}

			return PartialView(model);
		}

		/// <summary>
		///     Uploads the specified files.
		/// </summary>
		/// <param name="documentUpload">The upload.</param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult Upload(DocumentUpload documentUpload)
		{

			var success = false;
			string message = null;
			dynamic client = null;
			if (documentUpload.UploadId != null)
			{
				IHubContext uploadHubContext = GlobalHost.ConnectionManager.GetHubContext<UploadHub>();
				// Because not on page post back, can be removed if ever added to the page post back
				client = uploadHubContext.Clients.Client(documentUpload.UploadId);
				FileUploadWorkerProcess.ClientUpdateProgress(client, 90, "Checking for duplicate file...");
			}

			try
			{
				if (string.IsNullOrEmpty(documentUpload.OriginalFileName))
				{
					message = "Please select a file before upload.";
					success = false;
					throw new InvalidOperationException("Please select a file before upload.");
				}


				if (documentUpload.ReUpload)
				{
					var actualFileName = documentUpload.OriginalFileName;
					var actualContentSize = documentUpload.Size;
					IDictionary<string, string> metadata = _ariaProvider.FetchMetadata(EntityType.Document, AssetType.Document, documentUpload.AssetId);

					documentUpload.Metadata = metadata;
					if (System.String.Compare(documentUpload.Title, documentUpload.OriginalFileName, System.StringComparison.OrdinalIgnoreCase) == 0)
					{
						documentUpload.Title = actualFileName;
					}

					documentUpload.OriginalFileName = actualFileName;
					documentUpload.Size = actualContentSize;

					var doc = _fileTransferProvider.CheckIsDocumentExistsOnReUpload(documentUpload);
					if (doc != null && doc.AssetId != documentUpload.AssetId)
					{
						message = "A file with the same Name and Customer Access already exists.";
						success = false;
						throw new NotSupportedException(string.Format("{0} is a duplicate file.",
							documentUpload.NormalizedTile));
					}


					//Clearing model state errors. We don't want to validate on Re-upload. because we have modified metadata above. 
					foreach (var modelValue in ModelState.Values)
					{
						modelValue.Errors.Clear();
					}
				}
				else
				{

					if (!_userContext.CanSpecifyDocumentPermissions(documentUpload.ContainerId))
						documentUpload.Permission = DocumentPermission.Modify;

					var doc = _fileTransferProvider.CheckIsDocumentExists(documentUpload);
					if (doc != null)
					{
						if (documentUpload.Overwrite)
						{
							documentUpload.AssetId = doc.AssetId;
						}
						else
						{
							message = "A file with the same Name and Customer Access already exists.";
							success = false;
							throw new NotSupportedException(string.Format("{0} is a duplicate file.",
								documentUpload.NormalizedTile));
						}
					}
				}



				if (System.IO.File.Exists(documentUpload.TempFilePath))
				{
					ModelState.Remove("File");
				}
				if (documentUpload == null || !ModelState.IsValid)
					throw new InvalidOperationException("Missing required fields");

				//Set required properties
				documentUpload.LastModifiedBy = _userContext.LoginId;

				if (client != null)
				{
					FileUploadWorkerProcess.ClientUpdateProgress(client, 95, "Saving file...");
				}

				_fileTransferProvider.UploadDocument(documentUpload);
				success = true;
				message = "Upload completed successfully";
				if (client != null)
				{
					FileUploadWorkerProcess.ClientUpdateProgress(client, 100, "Upload completed successfully");
					FileUploadWorkerProcess.ClientComplete(client, documentUpload.OriginalFileName,
						"<strong>" + documentUpload.OriginalFileName +
						"</strong> was successfully uploaded.");
					var pageMessage = CreatePageMessage("<strong>" + documentUpload.OriginalFileName +
														"</strong> was successfully uploaded.", title: "Success!",
														severity: TraceEventType.Information);
					AddPageMessage(pageMessage);
				}
			}
			catch (Exception ex)
			{
				_logHelper.Log(MessageIds.ContainerControllerUploadException, LogCategory.FileTransfer, LogPriority.High, TraceEventType.Error, HttpContext, "Error saving uploaded file.", ex);

				if (client != null)
					FileUploadWorkerProcess.ClientError(client, "Upload failed: " + ex.GetBaseException().Message);

				success = false;
				if (message == null)
					message = ex.GetBaseException().Message;
				//throw;
			}
			finally
			{
				if (!string.IsNullOrEmpty(documentUpload.TempFilePath))
					System.IO.File.Delete(documentUpload.TempFilePath);
			}

			return Json(new { success, message });
		}

		/// <summary>
		/// Uploads multiple files.
		/// </summary>
		/// <param name="id">The unique identifier.</param>
		/// <param name="entityType">Type of the entity.</param>
		/// <param name="containerId">The container unique identifier.</param>
		/// <param name="reupload">The reupload.</param>
		/// <param name="assetId">The asset identifier.</param>
		/// <returns></returns>
		/// <exception cref="System.Web.HttpException">Container not found</exception>
		public ActionResult UploadMultiple(Guid id, EntityType entityType, Guid? containerId, bool? reupload = false, Guid? assetId = null)
		{
			if (containerId == null || containerId.Value == Guid.Empty)
				containerId = _searchProvider.GetContainerId(id, _sessionProvider, _userContext);

			if (containerId == null)
				throw new HttpException((int)HttpStatusCode.NotFound, "The requested container could not be found");

			var model = new DocumentUpload
			{
				EnableLogging = _portalConfiguration.EnableUploadUiLogging,
				ContainerId = containerId.Value,
				DocumentTypes = DocumentMetadataUpload.AuthorizedDocumentListItems(_portalConfiguration.DocumentTypes, false),
				UploadId = Guid.NewGuid().ToString("N"),
				LastModifiedBy = _userContext.LoginId,
				DocumentTypeId = _portalConfiguration.DocumentTypes.Last().Id,
				ReUpload = reupload.Value,
				AssetId = assetId.GetValueOrDefault()
			};

			if (_userContext.CanSpecifyDocumentPermissions(containerId.Value))
			{
				model.Permissions = GetDocumentPermissions(containerId.Value);
				model.Permission = DocumentPermission.Private;
			}

			return PartialView(model);
		}

		/// <summary>
		/// Gets the documents for a project's order and presents them for import to the project docs
		/// </summary>
		/// <param name="projectId">The project guid</param>
		/// <param name="containerId">The project container guid</param>
		/// <param name="orderNumber">The orderNumber</param>
		/// <returns></returns>
		[HttpGet]
		public ActionResult ImportProjectOrderDocuments(Guid projectId, Guid containerId, string orderNumber)
		{
			var model = new ImportProjectOrderDocumentsModel()
			{
				ProjectId = projectId,
				ProjectContainerId = containerId,
				OrderNumber = orderNumber,

			};

			if (!orderNumber.IsNullOrWhiteSpace())
			{
				var order = _searchProvider.Search<OrderSearchResult>(
					new SearchCriteria()
					{
						EntityType = EntityType.Order,
						Filters =
							new Dictionary<string, List<string>> { { "ariaOrderNumber", new List<string> { orderNumber } } }
					},
					_sessionProvider, _userContext).Results.FirstOrDefault();

				if (order != null)
				{
					var documentSearchResults = _searchProvider.FetchDocuments(order.ContainerId);
					model.ProjectOrderDocuments =
						documentSearchResults.Select(x => new ImportProjectOrderDocumentModel()
						{
							Id = x.Id,
							Name = x.Name,
							Title = x.Title,
							IconCssClass = x.IconCSSClass,
							DocumentTypeText = x.FormatDocumentType(_portalConfiguration)
						}).ToList();
				}
			}

			return PartialView(model);
		}

		/// <summary>
		/// Processes the import/copy of selected documents from a project's order to the project's docs
		/// </summary>
		/// <param name="importProjectOrderDocumentsModel">The model for this action.</param>
		/// <param name="documentIds">The document identifiers to import/copy.</param>
		/// <returns></returns>
		[HttpPost]
		public JsonResult ImportProjectOrderDocuments(ImportProjectOrderDocumentsModel importProjectOrderDocumentsModel, List<string> documentIds)
		{
			try
			{
				if (documentIds == null)
					throw new Exception("There are no documents selected to import.");

				foreach (var documentId in documentIds)
				{
					_fileTransferProvider.CopyDocument(documentId, importProjectOrderDocumentsModel.ProjectContainerId.ToString("N"));
				}
				ViewBag.Success = true;
				AddPageMessage(CreatePageMessage("Documents successfully imported.", title: "Success!"));
				return Json(new
				{
					success = true
				});
			}
			catch (Exception exception)
			{
				ViewBag.Success = false;
				AddPageMessage(CreatePageMessage("There is an error while importing documents from order." + exception.GetBaseException().Message, title: "Error!", severity: TraceEventType.Error));
				_logHelper.Log(MessageIds.BaseControllerServiceError, LogCategory.Project, LogPriority.High, TraceEventType.Error, this.HttpContext, "There is an error while importing documents from order.", exception);
				return Json(new
				{
					success = false
				});
			}

		}

		/// <summary>
		/// Return populated DocumentMetadataUpload model to UI
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <returns></returns>
		/// <exception cref="System.Web.HttpException">The requested container could not be found</exception>
		public ActionResult EditMetadata(Guid id)
		{
			IDictionary<string, string> metadata = _ariaProvider.FetchMetadata(EntityType.Document, AssetType.Document,
				id);

			var model = new DocumentMetadataUpload
			{
				Metadata = metadata,
				DocumentTypes =
					DocumentMetadataUpload.AuthorizedDocumentListItems(_portalConfiguration.DocumentTypes),
				AssetId = id
			};

			if (model.ContainerId == Guid.Empty)
			{
				_logHelper.Log(MessageIds.ContainerControllerContainerNotFound, LoggingCategory, LogPriority.High, TraceEventType.Warning, HttpContext, "Unable to find container ID for document " + id);
				throw new HttpException((int)HttpStatusCode.NotFound, "The requested container could not be found");
			}

			if (_userContext.CanSpecifyDocumentPermissions(model.ContainerId))
			{
				model.Permissions = GetDocumentPermissions(model.ContainerId);
			}

			return PartialView(model);
		}

		/// <summary>
		/// </summary>
		/// <param name="metadataUpload"></param>
		/// <returns></returns>
		[HttpPost]
		public JsonResult EditMetadata(DocumentMetadataUpload metadataUpload)
		{
			var message = new GrowlMessage();
			bool success = false;
			if (ModelState.IsValid)
			{
				try
				{
					_ariaProvider.UpdateMetadata(EntityType.Document, AssetType.Document, metadataUpload.AssetId,
						metadataUpload.ContainerId, metadataUpload.Metadata, _userContext);

					var text =
						string.Format(
							"<strong>{0}</strong> has been updated successfully. ",
							metadataUpload.Title ?? "Your document");
					message = CreatePageMessage(text, title: "Success!", severity: TraceEventType.Information);

					success = true;
				}
				catch (Exception exception)
				{
					var ex = exception.GetBaseException() as WebException;
					var cannotEditMetadata = ex != null && (ex.Response as HttpWebResponse) != null && (ex.Response as HttpWebResponse).StatusCode == HttpStatusCode.Conflict;
					_logHelper.Log(MessageIds.ContainerControllerEditMetaDataException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext, "Error editing document metadata", ex);
					message = cannotEditMetadata
						? CreatePageMessage("A file with the same Name and Customer Access already exists.", title: "Error", severity: TraceEventType.Error,
							sticky: true)
						: CreatePageMessage("An error occurred editing document properties." + exception.GetBaseException().Message,
							title: "Error", severity: TraceEventType.Error, sticky: true);
					_logHelper.Log(MessageIds.DocumentTemplateException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext, "Error while Editing Document Properties", exception);
					success = false;
				}

				AddPageMessage(message);
			}


			return Json(new
			{
				success,
				message
			});

		}

		/// <summary>
		/// Res the upload.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <returns></returns>
		/// <exception cref="System.Web.HttpException">The requested container could not be found</exception>
		public ActionResult ReUpload(Guid id)
		{
			IDictionary<string, string> metadata = _ariaProvider.FetchMetadata(EntityType.Document, AssetType.Document,
				id);

			var model = new DocumentUpload
			{
				EnableLogging = _portalConfiguration.EnableUploadUiLogging,
				Metadata = metadata,
				DocumentTypes = DocumentMetadataUpload.AuthorizedDocumentListItems(_portalConfiguration.DocumentTypes),
				UploadId = Guid.NewGuid().ToString("N"),
				LastModifiedBy = _userContext.LoginId,
				AssetId = id,
				ReUpload = true
			};

			if (model.ContainerId == Guid.Empty)
				throw new HttpException((int)HttpStatusCode.NotFound, "The requested container could not be found");

			return PartialView("Upload", model);
		}

		internal SelectList GetDocumentPermissions(Guid containerId)
		{
			var container = _containerProvider.FetchById(containerId);
			var list = container.ContainerLists.Where(x => x.Permissions.Any());
			var permissions = (from DocumentPermission permission in Enum.GetValues(typeof(DocumentPermission))
							   where list.Any(x => x.Name == permission.ToString())
							   select new SelectListItem
							   {
								   Text = permission.GetDisplayName(),
								   Value = permission.ToString()
							   }).ToList();

			return new SelectList(permissions.ToList(), "Value", "Text");
		}

		/// <summary>
		///     Deletes the document.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <returns></returns>
		public JsonResult DeleteDocument(Guid id)
		{
			GrowlMessage message;
			bool success;
			var document = new DocumentSearchResult();

			//
			// fetch the document to ensure it exists and to check for permissions
			//
			try
			{
				document.Metadata = _ariaProvider.FetchMetadata(EntityType.Document, AssetType.Document, id);
				document.ApplyPermissions(_userContext);
			}
			catch (Exception fetchEx)
			{
				_logHelper.Log(MessageIds.ContainerControllerDeleteDocumentFetchException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext, "Error fetching document for delete", fetchEx);
				document = null;
			}

			if (document == null)
			{
				message = CreatePageMessage("The document has already been deleted.  It may take a minute before this is reflected on the site", title: "Error", severity: TraceEventType.Error);
				success = false;
			}
			else if (!document.CanEdit)
			{
				message = CreatePageMessage("You do not have permission to delete this document.", title: "Error", severity: TraceEventType.Error);
				success = false;
			}
			else
			{
				try
				{
					_documentProvider.Delete(document.ContainerId, id);
					message = CreatePageMessage("This document has been deleted successfully.", title: "Success!", severity: TraceEventType.Start);
					success = true;
				}
				catch (Exception ex)
				{
					_logHelper.Log(MessageIds.ContainerControllerDeleteDocumentException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext, "Error while attempting to delete document " + id, ex);
					success = false;
					message = CreatePageMessage("There was an error encountered attempting to delete the document. The document may already be deleted.", title: "Error", severity: TraceEventType.Error);
				}
			}

			AddPageMessage(message);

			return Json(new
			{
				success,
				message,
				noRefresh = false
			});
		}

		/// <summary>
		///     Downloads a document by guid.
		/// </summary>
		/// <param name="documentId">The document id.</param>
		/// <returns></returns>
		public ActionResult DownloadDocument([Bind(Prefix = "Id")] string documentId)
		{
			FileDownload result = _fileTransferProvider.DownloadDocument(documentId);
			if (null == result)
				throw new HttpException((int)HttpStatusCode.NotFound, "Requested file could not be found");
			return File(result.DocumentStream, result.ContentType, result.FileName);
		}


		/// <summary>
		/// Edits the document online.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <returns></returns>
		[HttpPost]
		public JsonResult EditDocumentOnline(Guid id)
		{
			bool success;
			string message = "";
			try
			{
				_documentProvider.PrepareDocumentForEdit(id, true);
				success = true;
			}
			catch (Exception ex)
			{
				_logHelper.Log(MessageIds.ContainerControllerDeleteDocumentException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext, "Error while attempting to edit document " + id, ex);
				success = false;
				message = "Error while attempting to edit document. " + ex.GetBaseException().Message;
			}
			return Json(new { Success = success, Message = message });
		}

		/// <summary>
		/// Edits the document online.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <returns></returns>
		[HttpPost]
		public JsonResult PreviewDocumentOnline(Guid id)
		{
			bool success;
			string message = "";
			try
			{
				_documentProvider.PrepareDocumentForEdit(id, false);
				success = true;
			}
			catch (Exception ex)
			{
				_logHelper.Log(MessageIds.ContainerControllerDeleteDocumentException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext, "Error while attempting to preview document " + id, ex);
				success = false;
				message = "Error while attempting to preview document. " + ex.GetBaseException().Message;
			}
			return Json(new { Success = success, Message = message });
		}



		/// <summary>
		/// Unlocks the document.
		/// </summary>
		/// <param name="id">The identifier.</param>
		/// <returns></returns>
		[HttpPost]
		public JsonResult UnlockDocument(Guid id)
		{
			bool success;
			string message = "";
			GrowlMessage pagemessage;
			try
			{
				_documentProvider.UnlockDocument(id);
				message = "Document has been unlocked. It may take a minute before this is reflected on the site";
				pagemessage = CreatePageMessage(message);
				success = true;
			}
			catch (Exception ex)
			{
				_logHelper.Log(MessageIds.ContainerControllerDeleteDocumentException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext, "Error while attempting to unlock document " + id, ex);
				success = false;
				message = "Error while attempting to unlock document. " + ex.GetBaseException().Message;
				pagemessage = CreatePageMessage(message);
				
			}
			AddPageMessage(pagemessage);

			return Json(new
			{
				success,
				pagemessage,
				noRefresh = false
			});
		}
	}
}