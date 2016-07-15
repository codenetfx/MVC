using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using UL.Aria.Common.Framework;
using UL.Aria.Web.Portal.Logging;
using UL.Enterprise.Foundation.Logging;
using UL.Aria.Service.Contracts.Dto;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Profile;
using UL.Aria.Web.Common.Models.Search;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Providers;
using System.Linq;

namespace UL.Aria.Web.Portal.Controllers
{
    /// <summary>
    /// Profile controller class.
    /// </summary>
    [Authorize]
    public class ProfileController : BaseController
    {
        /// <summary>
        /// The _profile provider
        /// </summary>
        private readonly IProfileProvider _profileProvider;

        private readonly ISessionProvider _sessionProvider;
        private readonly ICompanyProvider _companyProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ProfileController" /> class.
        /// </summary>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="profileProvider">The profile provider.</param>
        /// <param name="userContext">The user context.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="sessionProvider">The session provider.</param>
        /// <param name="companyProvider">The company provider.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
        public ProfileController(ILogHelper logHelper, IProfileProvider profileProvider, IUserContext userContext,
            IPortalConfiguration portalConfiguration, ISessionProvider sessionProvider, ICompanyProvider companyProvider, 
            ITempDataProviderFactory tempDataProviderFactory)
            : base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
        {
            _profileProvider = profileProvider;
            _sessionProvider = sessionProvider;
            _companyProvider = companyProvider;
        }

        /// <summary>
        /// Gets the logging category to use for all logging.
        /// </summary>
        /// <value>
        /// The logging category.
        /// </value>
        protected override LogCategory LoggingCategory
        {
            get { return LogCategory.User; }
        }

        /// <summary>
        /// Returns the default profile list.
        /// </summary>
        /// <returns>ActionResult</returns>
        public ActionResult Index()
        {
            var result = _profileProvider.FetchById(_userContext.UserId);

            result.PageActions = PageActions();
            result.PageLinks = PageLinks();
            result.Breadcrumbs = Breadcrumbs(null);
            return View(result);
        }

        /// <summary>
        /// Avitars the specified id.
        /// </summary>
        /// <returns>System.Web.Mvc.ActionResult.</returns>
        public ActionResult Avatar()
        {
            // following code commented out until we support uploading these files.
            //var result = _profileProvider.FetchImageById(_userContext.UserId);

            //if (result.Image != null && result.Image.Length > 0)
            //    return File(result.Image, result.ContentType);
            //else
            return File("~/images/Default_Avatar.png", "image/png");
        }

        /// <summary>
        /// Searches the ul users.
        /// </summary>
        /// <param name="keyword">The keyword.</param>
        /// <returns></returns>
        public ActionResult SearchULUsers(string keyword)
        {
            var results = SearchUserProfiles(keyword);

            return Json(new JsonResponseModel()
            {
                Successful = true,
                Data = results.Select(x => new
                {
                    Id = x.Id,
                    Display = x.BasicInformation.LoginId,
                    Description = x.BasicInformation.DisplayName
                }),
                ErrorCode = 200
            });
        }

        private ActionResult CreateUserProfileSearchResponseModel(IEnumerable<UserProfile> results)
        {
            return Json(new JsonResponseModel()
            {
                Successful = true,
                Data = results.Select(x => new
                {
                    Id = x.BasicInformation.LoginId,
                    Display = x.BasicInformation.DisplayName,
                    Description = x.BasicInformation.LoginId
                }),
                ErrorCode = 200
            });
        }

        private IEnumerable<UserProfile> SearchUserProfiles(string keyword)
        {
            var results = _profileProvider.Search(new UserProfileSearchSpecificationModel()
            {
                Keyword = keyword,
                CompanyId = _companyProvider.GetULCompanyId()
            });
            return results;
        }

        /// <summary>
        /// Searches the eligible task owners.
        /// </summary>
        /// <param name="keyword">The keyword.</param>
        /// <returns></returns>
        public ActionResult SearchEligibleTaskOwners(string keyword)
        {
            var searchResult = SearchUserProfiles(keyword).ToList();
            var lowerInvariant = keyword.ToLowerInvariant();    
            if ("my tasks".Contains(lowerInvariant))
            {
                searchResult.Insert(0, new UserProfile{Id=_userContext.UserId,  BasicInformation = new BasicInfo{ LoginId=_userContext.LoginId, DisplayName = "My Tasks"}});
            }
            if ("unassigned".Contains(lowerInvariant))
            {
                searchResult.Insert(0, new UserProfile { Id = null, BasicInformation = new BasicInfo { LoginId = "unassigned", DisplayName = "Unassigned" } });
            }

            if (_portalConfiguration.TaskReviewGroupEmail.ToLower().Contains(lowerInvariant))
            {
                searchResult.Insert(0, new UserProfile { Id = null, BasicInformation = new BasicInfo { LoginId = _portalConfiguration.TaskReviewGroupEmail, DisplayName = "Awaiting Review" } });
            }
            
            return CreateUserProfileSearchResponseModel(searchResult);
        }

        /// <summary>
        /// Searches the eligible project owners.
        /// </summary>
        /// <param name="keyword">The keyword.</param>
        /// <returns></returns>
        public ActionResult SearchEligibleProjectOwners(string keyword)
        {
            var searchResult = SearchUserProfiles(keyword).ToList();
            var lowerInvariant = keyword.ToLowerInvariant();
            if ("assigned to me".Contains(lowerInvariant)
                ||
                "my projects".Contains(lowerInvariant)
                )
            {
                searchResult.Insert(0, new UserProfile { Id = _userContext.UserId, BasicInformation = new BasicInfo { LoginId = _userContext.LoginId, DisplayName = "Assigned to me" } });
            }

            return CreateUserProfileSearchResponseModel(searchResult);
        }

        /// <summary>
        /// basic profile editing.
        /// </summary>
        /// <returns></returns>
        public ActionResult Basic()
        {
            var result = _profileProvider.FetchById(_userContext.UserId);
            result.PageActions = PageActions();
            result.PageLinks = PageLinks();
            result.Breadcrumbs = Breadcrumbs("Edit Profile");
            return View(result);
        }

        /// <summary>
        /// basic profile editing.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Basic(UserProfile model)
        {
            //ignore errors from email - this form does not edit it
            ModelState.Remove("BasicInformation.LoginId");

            if (ModelState.IsValid && model.BasicInformation != null)
            {
                return TryServiceCall(model,
                                    () =>
                                    _profileProvider.Update(_userContext.UserId, model.BasicInformation, Session, _userContext),
                                    "Basic profile information saved successfully",
                                    "Error saving basic profile information: {0}");
            }
            return View(model);
        }


        private IEnumerable<TaxonomyMenuItem> PageActions()
        {
            var list = new List<TaxonomyMenuItem>();

            list.Add(new TaxonomyMenuItem
                {
                    Text = "Edit Profile",
                    Url = Url.PageProfileBasic(),
                    CssClass = "arrow primary"
                });

            return list;
        }

        /// <summary>
        /// A page showing user's scratch documents
        /// </summary>
        /// <returns></returns>
        public ActionResult ScratchSpace()
        {
            var model = _profileProvider.FetchAllScratchFiles(_userContext);

            var crumbs = Breadcrumbs("Scratch Space");
            //  crumbs.Add(new Breadcrumb { Text = , Url = Url.PageScratchSpace() });
            model.Breadcrumbs = crumbs;

            model.PageLinks = PageLinks();
            return View(model);
        }


        /// <summary>
        /// Purces the scratch space.
        /// </summary>
        /// <returns></returns>
        public ActionResult PurgeScratchSpace()
        {
            _profileProvider.PurgeScratchSpace(_userContext);

            return RedirectToAction("ScratchSpace");
        }

        /// <summary>
        /// Uploads the specified files.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult Upload(DocumentUpload file)
        {
            try
            {
                _profileProvider.UploadScratchFile(file, _userContext);
            }
            catch (Exception ex)
            {
                _logHelper.Log(MessageIds.ProfileControllerScratchFileUploadError, LogCategory.FileTransfer, LogPriority.High, TraceEventType.Error, HttpContext,
                                "Error saving uploaded scratch file.", ex);
            }
            finally
            {
                System.IO.File.Delete(file.TempFilePath);
            }

            return new EmptyResult();
        }


        /// <summary>
        /// Creates the favorite search.
        /// </summary>
        /// <param name="criteria">The criteria.</param>
        /// <param name="location">The location.</param>
        /// <returns></returns>
        public ActionResult CreateFavoriteSearch(SearchCriteria criteria, string location)
        {
            ViewBag.Success = false;
            var vm = new SearchFavorite()
            {
                Criteria = criteria,
                PageUrl = location
            };

            vm = _profileProvider.BuildSearchCriteriaDisplay(vm, _userContext);

            return PartialView("CreateFavoriteSearch", vm);
        }

        /// <summary>
        /// Creates the default search.
        /// </summary>
        /// <param name="searchFavorite">The search favorite.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult CreateDefaultSearch(SearchFavorite searchFavorite)
        {

            searchFavorite.AvailableDefault = true;
            searchFavorite.ActiveDefault = true;
            try
            {
                if (string.IsNullOrEmpty(searchFavorite.Title))
                    throw new Exception("Name is required.");

                _profileProvider.CreateFavoriteSearch(_userContext.UserId, searchFavorite, Session, _sessionProvider, _userContext);
                string message = string.Format("Default search <strong> {0} </strong>has been saved.", searchFavorite.Title);
                AddPageMessage(CreatePageMessage(message, title: "Success!", severity: TraceEventType.Information));
            }
            catch (Exception)
            {
                string message = string.Format("There is an error while saving default search <strong>{0}</strong>.", searchFavorite.Title);
                AddPageMessage(CreatePageMessage(message, title: "Error!", severity: TraceEventType.Error));
                throw;
            }
            return Redirect(Request.UrlReferrer.ToString());
        }
        /// <summary>
        /// Creates the favorite search.
        /// </summary>
        /// <param name="searchFavorite">The search favorite.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult CreateFavoriteSearch(SearchFavorite searchFavorite)
        {
            ViewBag.Success = false;

            if (ModelState.IsValid)
            {
                try
                {
                    _profileProvider.CreateFavoriteSearch(_userContext.UserId, searchFavorite, Session, _sessionProvider, _userContext);
                    ViewBag.Success = true;
                    var message = string.Format("Favorite <strong>{0}</strong> was successfully created.", searchFavorite.Title);
                    AddPageMessage(CreatePageMessage(message, title: "Success!", severity: TraceEventType.Information));
                }
                catch (Exception ex)
                {
                    _logHelper.LogError(HttpContext, ex);
                    ModelState.AddModelError("", ex.GetBaseException().Message);
                }
            }

            if (!ViewBag.Success)
            {
                searchFavorite = _profileProvider.BuildSearchCriteriaDisplay(searchFavorite, _userContext);
            }

            return PartialView("CreateFavoriteSearch", searchFavorite);
        }

        /// <summary>
        /// Edits the favorite.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        public ActionResult EditFavorite(Guid id)
        {
            ViewBag.Success = false;
            var vm = _profileProvider.FetchSearchFavorite(_userContext.UserId, id, _userContext);
            return PartialView("EditFavoriteSearch", vm);
        }

        /// <summary>
        /// Edits the favorite.
        /// </summary>
        /// <param name="searchFavorite">The search favorite.</param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult EditFavorite(SearchFavorite searchFavorite)
        {
            ViewBag.Success = false;

            if (ModelState.IsValid)
            {
                try
                {
                    _profileProvider.UpdateSearchFavorite(_userContext.UserId, searchFavorite, Session, _sessionProvider, _userContext);

                    ViewBag.Success = true;
                    var message = string.Format("Favorite <strong>{0}</strong> was successfully updated.", searchFavorite.Title);
                    AddPageMessage(CreatePageMessage(message, title: "Success!", severity: TraceEventType.Information));
                }
                catch (Exception ex)
                {
                    _logHelper.LogError(HttpContext, ex);
                    ModelState.AddModelError("", ex.GetBaseException().Message);
                }
            }

            if (!ViewBag.Success)
            {
                searchFavorite = _profileProvider.BuildSearchCriteriaDisplay(searchFavorite, _userContext);
            }

            return PartialView("EditFavoriteSearch", searchFavorite);
        }


        /// <summary>
        /// Deletes the favorite.
        /// </summary>
        /// <param name="id">The id.</param>
        /// <returns></returns>
        public JsonResult DeleteFavorite(Guid[] id)
        {
            var message = new GrowlMessage();
            bool success = true;
            int errorCount = 0;
            if (id.Length > 0)
            {
                foreach (var guid in id)
                {
                    try
                    {
                        _profileProvider.DeleteSearchFavorite(_userContext.UserId, guid);
                        _sessionProvider.RemoveGroupItem(guid, EntityType.Favorite.ToString());
                    }
                    catch (Exception exception)
                    {
                        errorCount++;
                        _logHelper.Log(MessageIds.ProfileControllerDeleteFavoriteException, LogCategory.Profile, LogPriority.High, TraceEventType.Error, HttpContext,
                                        "There is an error while deleting favorite", exception);
                    }
                }
            }

            _profileProvider.UpdateSessionStash(Session, _sessionProvider, _userContext);


            if (errorCount == 0)
            {
                success = true;
                message = CreatePageMessage("Favorite(s) have been deleted successfully.", title: "Success!",
                                            severity: TraceEventType.Start);
            }
            else
            {
                success = false;
                message = CreatePageMessage(string.Format("There was an error deleting  Favorite(s). {0} of {1} errored.", errorCount, id),
                                            title: "Error", severity: TraceEventType.Error);
            }


            return Json(new
            {
                success,
                message
            });
        }

        /// <summary>
        /// Deletes the favorite group.
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        public ActionResult DeleteFavoriteGroup()
        {
            var hashSetIds = _sessionProvider.GetGroupItems(EntityType.Favorite.ToString());
            return DeleteFavorite(hashSetIds.ToArray());
        }


        /// <summary>
        /// Provides a ping method for long running user interaction with javascript.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ActionResult JsKeepAlive()
        {
            return Json(new { Successful = true });
        }


        /// <summary>
        /// Favoriteses this instance.
        /// </summary>
        /// <returns></returns>
        public ActionResult Favorites(SearchCriteria searchCriteria)
        {
            searchCriteria.ApplyFavoriteSearch();
            var selectedItems = _sessionProvider.GetGroupItems(EntityType.Favorite.ToString());
            SearchResultSet<SearchFavorite> model = _profileProvider.SearchFavorites(_userContext.UserId, searchCriteria, selectedItems, _userContext);
            model.PageLinks = PageLinks();
            model.Breadcrumbs = Breadcrumbs("Favorites");
            return View(model);
        }

        private List<Breadcrumb> Breadcrumbs(string breadCrumbText)
        {
            var breadCrumbs = new List<Breadcrumb>() {
				new Breadcrumb() {Text = "Home", Url = Url.PageHome()},
				new Breadcrumb() {Text = "My Profile", Url = Url.PageProfile()}
			};

            if (!string.IsNullOrEmpty(breadCrumbText))
            {
                breadCrumbs.Add(new Breadcrumb() { Text = breadCrumbText, Url = Url.Action(null) });
            }

            SetPageMetadata(breadCrumbText, breadCrumbs);

            return breadCrumbs;
        }

        internal List<TaxonomyMenuItem> PageLinks()
        {
            var list = new List<TaxonomyMenuItem>();

            list.Add(new TaxonomyMenuItem() { Key = "profile", Text = "Profile", Url = Url.PageProfile() });
            list.Add(new TaxonomyMenuItem() { Key = "favorites", Text = "Favorites", Url = Url.PageFavorites() });
            list.Add(new TaxonomyMenuItem() { Key = "scratchSpace", Text = "Scratch Space", Url = Url.PageScratchSpace() });
            return list;
        }
    }
}