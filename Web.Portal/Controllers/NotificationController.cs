using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization.Formatters.Binary;
using System.Web.Http;
using System.Web.Mvc;
using Newtonsoft.Json;
using UL.Aria.Service.Contracts.Service;
using UL.Aria.Common;
using UL.Aria.Web.Common.Mvc;
using UL.Enterprise.Foundation;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Notification;
using UL.Aria.Service.Contracts.Dto;
using UL.Aria.Web.Common.Models.Shared;
using System.Web;
using UL.Enterprise.Foundation.Framework;
using UL.Aria.Web.Common.Models.Search;
using UL.Aria.Web.Common.Notification;

namespace UL.Aria.Web.Portal.Controllers
{
	/// <summary>
	/// Provides a mvc controller for Notification related information.
	/// </summary>
	[Authorize()]
	public class NotificationController : BaseController
	{
		private readonly INotificationService _notificationService;
		private readonly INotificationItemEntityDataStrategyFactory _entityStrategyFactory;

        /// <summary>
        /// Initializes a new instance of the <see cref="NotificationController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="notificationService">The notification service.</param>
        /// <param name="entityStrategyFactory">The entity strategy factory.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
		public NotificationController(IUserContext userContext, ILogHelper logHelper, IPortalConfiguration portalConfiguration,
			INotificationService notificationService, INotificationItemEntityDataStrategyFactory entityStrategyFactory,
            ITempDataProviderFactory tempDataProviderFactory)
			: base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
		{
			_notificationService = notificationService;
			_entityStrategyFactory = entityStrategyFactory;

		}

		/// <summary>
		/// Gets the by current user.
		/// </summary>
		/// <param name="request">The request.</param>
		/// <returns></returns>
		public ActionResult GetByCurrentUser(NotificationRequestModel request)
		{
			request = AssureNotificationCriteriaInitialized(request);
			var results = GetNotificationResults(request);

			//get the entity taxonomy and filter first so that notification types are reduced.
			var entityTaxonomies = GetEntityTaxonamyList(results, request.EntityFilter,  request.NotificationFilter);
			results = FilterItemsByEntityType(request, results);
			var taxonomies = GetTaxonamyList(results, request.NotificationFilter, request.EntityFilter);

			results = FilterItemsByNotificationType(request, results);
			var items = MapResults(results);
			request.Criteria.Paging = SetupPagingModel(request.Criteria.Paging, items);
			items = ApplySorting(request.CurrentSort, items);

			items = items.GetRange((int)request.Criteria.Paging.StartIndex,
					(request.Criteria.Paging.StartIndex + request.Criteria.Paging.PageSize > items.Count - 1)
						? (int)(items.Count - request.Criteria.Paging.StartIndex)
						: request.Criteria.Paging.PageSize);

			RunNotificationEntityStrategies(items);

			return Json(new NotificationModel()
			{
				Successful = true,
				Items = items,
				TaxonomyList = taxonomies,
				EntityList = entityTaxonomies,
				Request = request,
				ErrorCode = 200,
				PagingHtmlString = RenderPartialViewToString("~/Views/Shared/_Paging.cshtml", request.Criteria.Paging)
			});
		}


		private void RunNotificationEntityStrategies(List<NotificationItemModel> items)
		{
			var suportedEntitytypes = new List<EntityTypeEnumDto> { EntityTypeEnumDto.Project, EntityTypeEnumDto.Task };
			var strategies = _entityStrategyFactory.GetStrategies(suportedEntitytypes);

			if (strategies != null)
			{
				items.ForEach(x =>
				{
					if (strategies.ContainsKey(x.EntityType.ToString()))
						strategies[x.EntityType.ToString()].UpdateEntitySpecificData(x, this.Url);
				});
			}
		}

		internal List<NotificationItemModel> ApplySorting(Sort sort, List<NotificationItemModel> items)
		{
			Func<NotificationItemModel, string> sortfunc = null;

			switch (sort.FieldName)
			{
				case AssetFieldNames.AriaNotificationTitle:
					sortfunc = (NotificationItemModel x) => x.Body.ToLower();
					break;
				case AssetFieldNames.AriaNotificationDate:
					sortfunc = (NotificationItemModel x) => x.Date;
					break;
				case AssetFieldNames.AriaNotificationType:
					sortfunc = (NotificationItemModel x) => x.NotificationType.GetDisplayName().ToLower();
					break;
			}

			if (sortfunc != null)
			{
				switch (sort.Order)
				{
					case SortDirectionDto.Descending:
						return items.OrderByDescending(sortfunc).ToList();
					case SortDirectionDto.Ascending:
					default:
						return items.OrderBy(sortfunc).ToList();
				}
			}

			return items;
		}


		private List<NotificationDto> FilterItemsByNotificationType(NotificationRequestModel request, List<NotificationDto> results)
		{
			return results.Where(x => request.NotificationFilter == NotificationTypeDto.Undefined
					|| x.NotificationType == request.NotificationFilter).ToList();
		}

		private List<NotificationDto> FilterItemsByEntityType(NotificationRequestModel request, List<NotificationDto> results)
		{
			return results.Where(x => request.EntityFilter == EntityTypeEnumDto.Container
						|| x.EntityType == request.EntityFilter).ToList();
		}

		private NotificationRequestModel AssureNotificationCriteriaInitialized(NotificationRequestModel request)
		{
			if (request.Criteria == null)
				request.Criteria = new SearchCriteria().ApplyNotificationSearch();
			else if (request.Initialize)
				request.Criteria.ApplyNotificationSearch();

			request.CurrentSort = Aria.Common.Framework.Guard.Clone( GetCurrentOrDefaultSort(request.CurrentSort, request.Criteria));



			request.Criteria.UserId = base._userContext.UserId;

			if (!request.Criteria.Filters.ContainsKey(AssetFieldNames.AriaNotificationType))
				request.Criteria.Filters.Add(AssetFieldNames.AriaNotificationType, new List<string>() { NotificationTypeDto.Undefined.ToString() });

			return request;
		}

		private List<NotificationDto> GetNotificationResults(NotificationRequestModel request)
		{
			var criteria = request.Criteria;
			if (criteria.Filters.ContainsKey(AssetFieldNames.AriaTaskId))
			{
				var entityId = criteria.Filters[AssetFieldNames.AriaTaskId].First();
				return this._notificationService.FetchNotificationsByEntity(entityId)
					.Where(x => x.UserId == request.Criteria.UserId
						&& x.StartDate.GetValueOrDefault() <= DateTime.UtcNow).ToList();
			}
			else
			{
				var userIdStr = request.Criteria.UserId.GetValueOrDefault().ToString();
				return this._notificationService.FetchNotificationsByUser(userIdStr)
				   .Where(x => x.StartDate.GetValueOrDefault() <= DateTime.UtcNow).ToList();

			}
		}

		private List<NotificationItemModel> MapResults(List<NotificationDto> results)
		{
			var items = new List<NotificationItemModel>();

			results.ForEach(x =>
			{
				items.Add(new NotificationItemModel()
				{
					Id = x.Id.GetValueOrDefault(),
					Body = x.Body,
					Date = x.StartDate.GetValueOrDefault().ToString("MM/dd/yyyy"),
					NotificationType = x.NotificationType,
					NotificationTypeDisplay = x.NotificationType.GetDisplayName(),
					EntityName = x.EntityType.GetDisplayName(),
					EntityType = x.EntityType,
					EntityId = x.EntityId,
					ContainerId = x.ContainerId
				});

			});

			return items;
		}

		internal Sort GetCurrentOrDefaultSort(Sort current, SearchCriteria criteria)
		{
			if (current != null)
				return current;

			if (criteria.Sorts == null || criteria.Sorts.Count <= 0)
				return null;

			var tempSort = criteria.Sorts.FirstOrDefault(x => x.FieldName == criteria.SortField && x.Order == criteria.SortOrder);

			return (tempSort != null)
				? tempSort
				: criteria.Sorts.First();
		}


		internal Paging SetupPagingModel(Paging pageInfo, List<NotificationItemModel> items)
		{
			if (pageInfo == null)
				pageInfo = new Paging();

			pageInfo.TotalResults = items.Count;
			pageInfo.EndIndex = (pageInfo.PageSize > items.Count) ? items.Count - 1 : Math.Min(pageInfo.StartIndex + pageInfo.PageSize, items.Count) - 1;
			return pageInfo;
		}

		private List<JsonTaxonomyMenuItem> GetTaxonamyList(List<NotificationDto> items, NotificationTypeDto selected, EntityTypeEnumDto selectedEntity)
		{
			var entityName = (selectedEntity == EntityTypeEnumDto.Container) ? string.Empty : selectedEntity.GetDisplayName();
			var categories = new List<JsonTaxonomyMenuItem>();

			items.Select(x => x.NotificationType).Distinct().ToList()
				.ForEach(x =>
				{
					categories.Add(new JsonTaxonomyMenuItem()
					{
						Selected = (selected == x),
						Text = x.GetDisplayName(),
						Count = items.Where(y => y.NotificationType == x).Count(),
						Key = ((int)x).ToString()
					});
				});

			categories = categories.OrderBy(x => x.Text).ToList();

			categories.Insert(0, new JsonTaxonomyMenuItem()
			{
				Selected = (selected == NotificationTypeDto.Undefined),
				Text = "All " + entityName,
				Count = items.Count,
				Key = ((int)NotificationTypeDto.Undefined).ToString()
			});

			return categories;
		}

		private List<JsonTaxonomyMenuItem> GetEntityTaxonamyList(List<NotificationDto> items, EntityTypeEnumDto selected, NotificationTypeDto notificationType)
		{
			var categories = new List<JsonTaxonomyMenuItem>();

			items.Select(x => x.EntityType).Distinct().ToList()
				.ForEach(x =>
				{
					categories.Add(new JsonTaxonomyMenuItem()
					{
						Selected = (selected == x),
						Text = x.GetDisplayName(),
						Count = items.Where(y => y.EntityType == x).Count(),
						Key = ((int)x).ToString()
					});
				});

			categories = categories.OrderBy(x => x.Text).ToList();

			categories.Insert(0, new JsonTaxonomyMenuItem()
			{
				Selected = (selected == EntityTypeEnumDto.Container) && (notificationType == NotificationTypeDto.Undefined),
				Text = "All",
				Count = items.Count,
				Key = ((int)EntityTypeEnumDto.Container).ToString()
			});

			return categories;
		}

		/// <summary>
		/// Dismisses this instance.
		/// </summary>
		/// <param name="request">The request.</param>
		/// <returns></returns>
		public ActionResult Dismiss(NotificationDismissRequest request)
		{
			var ids = request.Ids;
			string msg = "Notification(s) has been dismissed.";
			var successful = true;

			try
			{
				foreach (string id in ids)
				{
					Guid temp = Guid.Empty;
					if (!string.IsNullOrWhiteSpace(id) && Guid.TryParse(id, out temp))
						_notificationService.Delete(id);
					else
					{
						msg = "Invalid Notification Id.";
						successful = false;
					}
				}
			}
			catch (Exception ex)
			{
				_logHelper.LogError(this.HttpContext, ex);
				msg = "An error occurred while trying to dismiss the notification.";
				successful = false;
			}

			return Json(new JsonResponseModel() { Successful = successful, Message = msg });
		}

		/// <summary>
		/// Views the entity.
		/// </summary>
		/// <returns></returns>
		public ActionResult ViewEntity(NotificationItemModel notification)
		{
			if (notification != null)
			{
				switch (notification.EntityType)
				{
					case EntityTypeEnumDto.Task:
						return RedirectToAction("Edit", "Task", new
						{
							id = notification.EntityId.ToString(),
							containerId = notification.ContainerId.ToString()
						});

					default:
						throw new HttpException((int)HttpStatusCode.NotAcceptable,
							"An unsupported Entity type was attempted to be viewed via notifications.");

				}
			}

			throw new HttpException((int)HttpStatusCode.NotFound,
						  "Entity not found for notification.");
		}


		/// <summary>
		/// Views the notification.
		/// </summary>
		/// <param name="notification">The notification.</param>
		/// <returns></returns>
		/// <exception cref="System.Web.HttpException">
		/// An unsupported Entity type was attempted to be viewed via notifications.
		/// or
		/// Entity not found for notification.
		/// </exception>
		public ActionResult ViewNotification(NotificationItemModel notification)
		{
			if (notification != null)
			{
				switch (notification.EntityType)
				{
					case EntityTypeEnumDto.Task:
						return Json( Url.PageViewTask(notification.EntityId,notification.ContainerId.Value), new JsonSerializerSettings());

					default:
						throw new HttpException((int)HttpStatusCode.NotAcceptable,
							"An unsupported Entity type was attempted to be viewed via notifications.");

				}
			}

			throw new HttpException((int)HttpStatusCode.NotFound,
						  "Entity not found for notification.");
		}
		/// <summary>
		/// Gets the logging category to use for all logging.
		/// </summary>
		/// <value>
		/// The logging category.
		/// </value>
		protected override UL.Enterprise.Foundation.Logging.LogCategory LoggingCategory
		{
			get { return UL.Enterprise.Foundation.Logging.LogCategory.User; }
		}


		

	}
}
