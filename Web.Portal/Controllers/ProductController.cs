using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

using Microsoft.AspNet.SignalR;
using Microsoft.Practices.Unity.Utility;

using UL.Aria.Common;
using UL.Aria.Common.Authorization;
using UL.Aria.Web.Portal.Logging;
using UL.Enterprise.Foundation;
using UL.Enterprise.Foundation.Logging;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Http;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Product;
using UL.Aria.Web.Common.Models.Search;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Providers;
using UL.Aria.Web.Common.SignalR;

namespace UL.Aria.Web.Portal.Controllers
{
    /// <summary>
    ///     ProductController
    /// </summary>
    [System.Web.Mvc.Authorize]
    public class ProductController : BaseController
    {
        private readonly ICompanyProvider _companyProvider;
        private readonly IProductFamilyProvider _productFamilyProvider;
        private readonly IProductProvider _productProvider;
        private readonly ISearchProvider _searchProvider;
        private readonly ISessionProvider _sessionProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProductController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="searchProvider">The profile provider.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="productProvider">The product provider.</param>
        /// <param name="companyProvider">The company provider.</param>
        /// <param name="productFamilyProvider">The product family provider.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="sessionProvider">The session provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
        public ProductController(IUserContext userContext, ISearchProvider searchProvider, ILogHelper logHelper,
            IProductProvider productProvider, ICompanyProvider companyProvider,IProductFamilyProvider productFamilyProvider,
            IPortalConfiguration portalConfiguration, ISessionProvider sessionProvider, ITempDataProviderFactory tempDataProviderFactory)
            : base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
        {
            _searchProvider = searchProvider;
            _productProvider = productProvider;
            _companyProvider = companyProvider;
            _productFamilyProvider = productFamilyProvider;
            _sessionProvider = sessionProvider;
        }

        /// <summary>
        ///     Gets the logging category to use for all logging.
        /// </summary>
        /// <value>
        ///     The logging category.
        /// </value>
        protected override LogCategory LoggingCategory
        {
            get { return LogCategory.Product; }
        }

	    private ProductDetail GetProductOr404(Guid id)
        {
            ProductDetail productVm = _productProvider.FetchById(id, _userContext);

            if (productVm == null)
				throw new HttpException((int)HttpStatusCode.NotFound, "The requested product could not be found");

            // always ensure that we have a container id
            if (productVm.ContainerId == Guid.Empty)
                productVm.ContainerId = _searchProvider.GetContainerId(productVm.Id, _sessionProvider, _userContext).Value;

            // check ACL for rights to view this product
            if (!_userContext.CanAccessProduct(productVm.ContainerId, productVm.CompanyId, SecuredActions.View))
                throw new HttpException((int) HttpStatusCode.Forbidden, "Not authorized to access this product");

            return productVm;
        }


        /// <summary>
        ///     Details the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        /// <exception cref="System.Web.HttpException">Container not found</exception>
        public ActionResult Details(Guid id)
        {
            var productVm = GetProductOr404(id);

            productVm.Breadcrumbs = Breadcrumbs(productVm, null);
            productVm.PageLinks = ActionsLeft(id);
            productVm.PageActions = ActionsRight(productVm);

            return View(productVm);
        }

        /// <summary>
        ///     Products the image.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        public ActionResult Image(Guid id)
        {
            return File("~/images/Default_Product.png", "image/png");
        }


        /// <summary>
        ///     Documents the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="criteria">The criteria.</param>
        /// <returns></returns>
        public ActionResult Documents(Guid id, SearchCriteria criteria)
        {
            var product = GetProductOr404(id);

            var model = new ProductDocuments(product)
            {
                PageLinks = ActionsLeft(id),
                PageActions = ActionsRight(product),
                Breadcrumbs = Breadcrumbs(product, "Documents")
            };

            Guid containerId = product.ContainerId == Guid.Empty
                ? _searchProvider.GetContainerId(product.Id, _sessionProvider, _userContext).Value
                : product.ContainerId;
           
            criteria.ApplyContainerFilter(containerId).ApplyDocumentSearch();
            model.DocumentResults = Search<DocumentSearchResult>(criteria,()=> _searchProvider.FetchDocuments(criteria, _sessionProvider, _userContext));
            model.DocumentResults.SearchCriteria.PrimaryRefiner = _searchProvider.BuildRefiners(model.PageLinks,
                model.DocumentResults.RefinerResults, criteria, _userContext);

            return View(model);
        }


        /// <summary>
        ///     Downloads the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        public FileStreamResult Download(Guid id)
        {
            ContentDownload fileData = _productProvider.DownloadProductById(id);

            return File(fileData.OutputStream, fileData.ContentType, fileData.FileName);
        }

        /// <summary>
        ///     Downloads the products for the family with the specified id.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        public FileStreamResult DownloadFamily(Guid id)
        {
            ContentDownload fileData = _productProvider.DownloadProductsByFamily(id);

            return File(fileData.OutputStream, fileData.ContentType, fileData.FileName);
        }


        /// <summary>
        ///     Uploads the product family.
        /// </summary>
        /// <returns></returns>
        public ActionResult UploadProductFamily()
        {
            var upload = new ProductUpload
           {
               EnableLogging = _portalConfiguration.EnableUploadUiLogging,
               UploadId = Guid.NewGuid().ToString("N"),
           };

            return View("UploadProductFamily", upload);
        }


        /// <summary>
        ///     Uploads the product family.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult UploadProductFamily(ProductUpload upload)
        {
            bool success = false;
            var errorMessages = new List<string>();

            if (upload != null && System.IO.File.Exists(upload.TempFilePath))
            {
                ModelState.Remove("File");
                ModelState.Remove("CompanyId");
            }

            if (upload == null || !ModelState.IsValid)
                throw new InvalidOperationException();

            dynamic client = null;
            if (upload.UploadId != null)
            {
                IHubContext uploadHubContext = GlobalHost.ConnectionManager.GetHubContext<UploadHub>();
                client = uploadHubContext.Clients.Client(upload.UploadId);
                FileUploadWorkerProcess.ClientUpdateProgress(client, 90, "Saving file...");
            }

            try
            {
                errorMessages = _productFamilyProvider.UploadProductFamily(upload).ToList();
                if (!errorMessages.Any())
                {
                    success = true;
                }
                if (client != null)
                {
                    FileUploadWorkerProcess.ClientUpdateProgress(client, 100, "Upload completed successfully");
                    FileUploadWorkerProcess.ClientComplete(client, upload.OriginalFileName,
                        "Upload completed successfully");
                }
            }
            catch (Exception ex)
            {
                success = false;
                errorMessages.Add("There is an error uploading product family");
                _logHelper.Log(MessageIds.ProductControllerUploadFamilyException, LogCategory.FileTransfer, LogPriority.High, TraceEventType.Error, HttpContext,
                               "Error saving uploaded file.", ex);
                FileUploadWorkerProcess.ClientError(client, "Upload failed: " + ex.GetBaseException().Message);
            }
            finally
            {
                System.IO.File.Delete(upload.TempFilePath);
            }

            return PartialView("ProductFamilyUpload",
                new ProductFamilyUpload {Success = success, Errors = errorMessages});
        }

        /// <summary>
        ///     Gets the product upload modal
        /// </summary>
        /// <returns></returns>
        public ActionResult Upload()
        {
            var upload = new ProductUpload
            {
                EnableLogging = _portalConfiguration.EnableUploadUiLogging,
                UploadId = Guid.NewGuid().ToString("N"),
                CanSeeFamilies = _userContext.CanAccessProductFamilies()
            };

            if (_userContext.CanCreateProductsForMultipleCompanies())
            {
                IList<CompanyInfo> companies = _companyProvider.FetchAll();
                upload.Companies = ProductUpload.GetCompanies(companies);
            }
            else
            {
                upload.CompanyId = _userContext.CompanyId.ToString();
            }
            return View("Upload", upload);
        }


        /// <summary>
        ///     Uploads the specified files.
        /// </summary>
        /// <param name="upload">The upload.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        [HttpPost]
        public ActionResult Upload(ProductUpload upload)
        {
            if (upload != null && System.IO.File.Exists(upload.TempFilePath))
                ModelState.Remove("File");

            if (upload == null || !ModelState.IsValid)
            {
                _logHelper.Log(MessageIds.ProductControllerUploadInvalid, LogCategory.FileTransfer, LogPriority.High, TraceEventType.Warning, HttpContext,
                    "Invalid ProductUpload model - blocking upload.");
                throw new InvalidOperationException();
            }

            dynamic client = null;
            if (upload.UploadId != null)
            {
                IHubContext uploadHubContext = GlobalHost.ConnectionManager.GetHubContext<UploadHub>();
                client = uploadHubContext.Clients.Client(upload.UploadId);
                FileUploadWorkerProcess.ClientUpdateProgress(client, 90, "Saving file...");
            }

            try
            {
                string id = _productProvider.UploadDocument(upload, _userContext);
                string status =
                    string.Format(
                        "<strong>{2}</strong> was successfully uploaded.  It may take a minute before your product(s) are available on the site.  Your confirmation code is <a href=\"{0}\">{1}</a>.  You may check the <a href=\"{0}\">status of your upload here</a>.",
                                              Url.PageProductUploadStatus(Guid.Parse(id)), id, upload.OriginalFileName);
                if (client != null)
                {
                    FileUploadWorkerProcess.ClientUpdateProgress(client, 100, "Upload completed successfully");
                    FileUploadWorkerProcess.ClientComplete(client, upload.OriginalFileName, status);
                }
            }
            catch (Exception ex)
            {
                _logHelper.Log(MessageIds.ProductControllerUploadException, LogCategory.FileTransfer, LogPriority.High, TraceEventType.Error, HttpContext,
                    "Error saving uploaded file.", ex);

                FileUploadWorkerProcess.ClientError(client, "Upload failed: " + ex.GetBaseException().Message);
            }
            finally
            {
                System.IO.File.Delete(upload.TempFilePath);
            }

            return new EmptyResult();
        }

        /// <summary>
        ///     Gets a list of uploaded products
        /// </summary>
        /// <returns></returns>
        public ActionResult Uploads(SearchCriteria searchCriteria)
        {
            searchCriteria.ApplyLastModifiedSort();
            SearchResultSet<ProductUploadSearchResult> model =
                _productProvider.GetProductUploadsByUserId(_userContext.UserId, searchCriteria);

            ViewBag.Breadcrumbs = Breadcrumbs(null, "Upload Status");
            model.PageLinks = HomeController.BuildPageLinks(_userContext, Url, EntityType.Product.ToString());
            return View("Uploads", model);
        }

        /// <summary>
        ///     Gets a list of products and product upload messages associated with a product upload
        /// </summary>
        /// <param name="id"></param>
        /// <param name="searchCriteria"></param>
        /// <returns></returns>
        public ActionResult UploadStatus(Guid id, SearchCriteria searchCriteria)
        {
            var model = new ProductUploadStatus(_userContext) {ProductUpload = GetProductUploadOr404(id)};

            searchCriteria = searchCriteria.ApplyProductNameSort();
            model.ProductUploadResultSearchResultSet = _productProvider.GetProductUploadStatus(id, searchCriteria);


            var uploadListingCrumb = new Breadcrumb {Text = "Upload Status", Url = Url.PageProductUploads()};
            model.Breadcrumbs = Breadcrumbs(null, model.ProductUpload.OriginalFileName, uploadListingCrumb,
                model.ProductUpload.Id.ToString("N"));
            model.PageLinks = HomeController.BuildPageLinks(_userContext, Url, EntityType.Product.ToString());

            return View("UploadStatus", model);
        }

        private ProductUpload GetProductUploadOr404(Guid id)
        {
            ProductUpload productUpload = _productProvider.GetProductUploadById(id);

            if (productUpload == null)
				throw new HttpException((int)HttpStatusCode.NotFound, "The requested product could not be found");

            if (productUpload.CreatedBy != _userContext.UserId)
                throw new HttpException((int) HttpStatusCode.Forbidden, "Not authorized to access this upload");

            return productUpload;
        }


        /// <summary>
        ///     Submits the Product.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <param name="overRide">The override.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Submit(Guid[] id, bool overRide = false)
        {
            bool success = false;
            var errorMessages = new Dictionary<string, List<string>>();
            var exceptionMessage = string.Empty;
            bool canOverride = _userContext.CanOverrideProduct();
            try
            {
                errorMessages = _productProvider.SubmitProducts(id.ToList(), overRide, _userContext);
                if (errorMessages != null && errorMessages.Count <= 0)
                {
                    success = true;
                    const string message = "Product has been successfully submitted.";
                    AddPageMessage(CreatePageMessage(message, title: "Success!"));
                }
            }
            catch (Exception ex)
            {
                _logHelper.Log(MessageIds.ProductControllerSubmitException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext,
                    "Error submitting products", ex);
                success = false;
                canOverride = false;
                exceptionMessage = "There is an error while submitting product: " + ex.GetBaseException().Message;
            }

            return PartialView("ProductSubmit",
                new ProductSubmit
                {
                    Success = success,
                    Errors = errorMessages,
                    Id = id,
                    CanOverRide = canOverride,
                    ExceptionMessage = exceptionMessage
                });
        }


        /// <summary>
        ///     Submits the product.
        /// </summary>
        /// <param name="productId">The product id.</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult SubmitProduct(Guid[] productId)
        {
            var message = new GrowlMessage();
            bool success = true;
            if (productId.Length > 0)
            {
                try
                {
                    //List<Guid> selectedProducts = products.ToList().ConvertAll(x => new Guid(x));
                    _productProvider.SubmitProducts(productId.ToList(), false, _userContext);

                    message = CreatePageMessage("Products have been submitted successfully.", title: "Success!",
                        severity: TraceEventType.Start);
                }
                catch (Exception ex)
                {
                    _logHelper.Log(MessageIds.ProductControllerSubmitException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext,
                        "Error submitting products", ex);
                    success = false;
                    message =
                        CreatePageMessage(
                            "There was an error submitting products. The product may already be deleted.",
                            title: "Error", severity: TraceEventType.Error);
                }
            }

            return Json(new
            {
                success,
                message
            });
        }

        /// <summary>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult Delete(Guid id)
        {
            GrowlMessage message = null;
            bool success = true;

            if (ModelState.IsValid)
            {
                try
                {
                    var product = GetProductOr404(id);
                    if (_userContext.CanEditProduct(product))
                    {
                        _productProvider.Delete(id);
                        AddPageMessage(
                            "Product has been deleted successfully.  It may take a minute before it is removed from the site.",
                            title: "Success!", severity: TraceEventType.Start);
                    }
                    else
                    {
                        success = false;
                        message = CreatePageMessage("You don't have permissions to delete this product.", title: "Error",
                            severity: TraceEventType.Error, sticky: true);
                    }
                }
                catch (Exception ex)
                {
                    _logHelper.Log(MessageIds.ProductControllerDeleteException, LoggingCategory, LogPriority.High, TraceEventType.Error, HttpContext,
                        "Error deleting product", ex);
                    success = false;
                    message =
                        CreatePageMessage(
                            "There was an error deleting this product. The product may already be deleted." + ex.Message,
                            title: "Error", severity: TraceEventType.Error, sticky: true);
                }
            }

            return Json(new
            {
                success,
                message
            });
        }

        /// <summary>
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public FileStreamResult UploadFile(Guid id)
        {
            ContentDownload fileData = _productProvider.DownloadProductUploadFileById(id);


            return File(fileData.OutputStream, fileData.ContentType, fileData.FileName);
        }

        internal List<TaxonomyMenuItem> ActionsLeft(Guid id)
        {
            return new List<TaxonomyMenuItem>
            {
                new TaxonomyMenuItem
                {
					Key = "productDetails",
					Text = "Overview",
					Url = Url.ProductDetails(id),
					CssClass = "empty"
				},
                new TaxonomyMenuItem
                {
                    Key = EntityType.Document.ToString(),
                    Text = "Documents",
                    Url = Url.ProductDocuments(id),
                    IsRefinable = true
                }
			};
        }

        internal List<TaxonomyMenuItem> ActionsRight(ProductDetail product)
        {
            Guid id = product.Id;
            bool isReadOnly = !product.CanEditProduct, canSubmitProduct = product.CanBeSubmitted;
            var actionsRight = new List<TaxonomyMenuItem>();

            if (canSubmitProduct)
            {
                actionsRight.Add(new TaxonomyMenuItem
                {
                    Key = "submitProduct",
                    Text = "Submit",
                    Url = "#",
                    DataUrl = Url.ProductSubmit(id),
                    Target = "SubmitConfirmModal",
                    ModalTitle = "Submit Product",
                    ModalText = string.Format("Are you sure you want to submit '<span>{0}</span>'?", product.Name),
                    CssClass = "arrow primary",
                    Modal = true
                });
            }

            actionsRight.Add(new TaxonomyMenuItem
            {
                Key = "download",
                Text = "Download",
                Url = Url.ProductDownload(id),
                CssClass = "arrow",
                IsRefinable = true
            });


            if (!isReadOnly)
            {
                actionsRight.Add(new TaxonomyMenuItem
                {
                    Key = "upload",
                    Modal = true,
                    Text = "Upload Documents",
                    Url = Url.UploadMultipleDocuments(id, EntityType.Product, product.ContainerId),
					CssClass = "arrow",
					ModalWidth = TaxonomyMenuItem.UploadModalWidth
                });
                actionsRight.Add(new TaxonomyMenuItem
                {
                    Key = "delete",
                    Text = "Delete Product",
                    Url = "#",
                    DataUrl = Url.ProductDelete(id),
                    ModalTitle = "Delete Product",
                    ModalText = string.Format("Are you sure you want to delete '<span>{0}</span>'?", product.Name),
                    Target = "DeleteModal",
                    CssClass = "arrow",
                    Modal = true,
                    ActionRedirect = Url.PageSearchProducts()
                });
            }

            return actionsRight;
        }

        internal IEnumerable<Breadcrumb> Breadcrumbs(ProductDetail product, string crumbText,
            Breadcrumb middleCrumb = null, string trackingCrumbText = null)
        {
            var trackingTitles = new List<Pair<string, string>>();
            var crumbs = new List<Breadcrumb> {new Breadcrumb {Text = "Products", Url = Url.PageSearchProducts()}};

            if (product != null)
            {
                var productName = product.Name;
                trackingTitles.Add(new Pair<string, string>(productName, product.Id.ToString("N")));
                crumbs.Add(new Breadcrumb {Text = productName, Url = Url.ProductDetails(product.Id)});
            }

            if (middleCrumb != null)
                crumbs.Add(middleCrumb);

            if (crumbText != null)
            {
                crumbs.Add(new Breadcrumb {Text = crumbText, Url = Url.Action(null)});
                ViewBag.SearchTitle = crumbText.Pluralize();
                if (trackingCrumbText != null)
                    trackingTitles.Add(new Pair<string, string>(crumbText, trackingCrumbText));
            }

            SetPageMetadata("Product", crumbs, trackingTitles: trackingTitles.ToArray());

            return ViewBag.BreadCrumbs;
        }

        private SearchResultSet<TResult> Search<TResult>(SearchCriteria searchCriteria, Func<SearchResultSet<TResult>> searchFunction ) where TResult : SearchResult
        {
            SearchResultSet<TResult> results = searchFunction!=null? searchFunction(): _searchProvider.Search<TResult>(searchCriteria, _sessionProvider, _userContext);
            results.PageLinks = HomeController.BuildPageLinks(_userContext, Url, results.SearchCriteria.EntityType.ToString());
			SetNoSearchResultsMessage(EntityType.Product);
            return results;
        }

        private SearchResultSet<TResult> Search<TResult>(SearchCriteria searchCriteria) where TResult : SearchResult
        {
            return Search<TResult>(searchCriteria,
                () => _searchProvider.Search<TResult>(searchCriteria, _sessionProvider, _userContext));
        }
    }
}
