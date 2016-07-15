using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Microsoft.Practices.Unity.Utility;
using UL.Enterprise.Foundation.Logging;
using UL.Aria.Web.Common;
using UL.Aria.Web.Common.Configuration;
using UL.Aria.Web.Common.Logging;
using UL.Aria.Web.Common.Models.Help;
using UL.Aria.Web.Common.Models.Shared;
using UL.Aria.Web.Common.Mvc;
using UL.Aria.Web.Common.Providers;

namespace UL.Aria.Web.Portal.Controllers
{
	/// <summary>
	///     HelpController
	/// </summary>
	[Authorize]
	public class HelpController : BaseController
	{
		private readonly IEmailProvider _emailProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="HelpController" /> class.
        /// </summary>
        /// <param name="userContext">The user context.</param>
        /// <param name="logHelper">The log helper.</param>
        /// <param name="emailProvider">The contact us provider.</param>
        /// <param name="portalConfiguration">The portal configuration.</param>
        /// <param name="tempDataProviderFactory">The temporary data provider factory.</param>
		public HelpController(IUserContext userContext, ILogHelper logHelper, IEmailProvider emailProvider,
            IPortalConfiguration portalConfiguration, ITempDataProviderFactory tempDataProviderFactory)
			: base(userContext, logHelper, portalConfiguration, tempDataProviderFactory)
		{
			_emailProvider = emailProvider;
		}

		/// <summary>
		///     Gets the logging category to use for all logging.
		/// </summary>
		/// <value>
		///     The logging category.
		/// </value>
		protected override LogCategory LoggingCategory
		{
			get { return LogCategory.Help; }
		}

        private const string helpText = "Need help? Please review our FAQs for a potential solution, then feel free to contact us if you can't find a relevant answer. If you have any suggestions as to how the FAQ section can be improved, please send them our way.";
		
		/// <summary>
		///     Indexes this instance.
		/// </summary>
		/// <returns></returns>
		public ActionResult Index()
		{
			return RedirectToAction("Faq");
		}


		/// <summary>
		///     Contacts the us.
		/// </summary>
		/// <returns></returns>
		public ActionResult ContactUs()
		{
			var vm = new ContactUsModel {
				PageLinks = GetPageLinks(),
				Breadcrumbs = SetPageMetadata("Contact Us"),
				IndustryList = _portalConfiguration.Industries,
				HelpText = helpText,
                ContactEmail =  _userContext.LoginId
			};

			ViewBag.Title = "Contact Us :: UL";
			ViewBag.PageTitle = "Contact Us";
			return View(vm);
		}

		/// <summary>
		///     Contacts the us.
		/// </summary>
		/// <param name="vm">The vm.</param>
		/// <returns></returns>
		[HttpPost]
		public ActionResult ContactUs(ContactUsModel vm)
		{
			ViewBag.Title = "Contact Us :: UL";
			ViewBag.PageTitle = "Contact Us";

			if (ModelState.IsValid)
			{
				EmailResponse response = _emailProvider.ContactUs(vm);
				if (response.Error)
				{
					ModelState.AddModelError(string.Empty, response.Message);
					return ContactUs();
				}
				return RedirectToAction("Confirmation");
			}
			return ContactUs();
		}

		/// <summary>
		///     Confirmations this instance.
		/// </summary>
		/// <returns></returns>
		public ActionResult Confirmation()
		{
			ViewBag.Title = "Contact Us :: UL";
			ViewBag.PageTitle = "Contact Us";

			var vm = new HelpSection {
				PageLinks = GetPageLinks(),
				Breadcrumbs = SetPageMetadata("Contact Us"),
				HelpText = helpText
			};

			return View(vm);
		}

		/// <summary>
		/// FAQs this instance.
		/// </summary>
		/// <returns></returns>
		public ActionResult Faq()
		{
			var model = new HelpSection {
				Breadcrumbs = SetPageMetadata("FAQs"), 
				PageLinks = GetPageLinks(),
				HelpText = helpText
			};

			var faqSections = new List<TaxonomyMenuItem> {
				new TaxonomyMenuItem {Text = "All ", Url = Url.PageFaq(), Count = 45, RefinementValue = "#" },
				new TaxonomyMenuItem {Text = "Login", Url = Url.PageFaqDetails("faq-login"), Count = 12, RefinementValue = "#" },
				new TaxonomyMenuItem {Text = "Home", Url = Url.PageFaqDetails("faq-home"), Count = 9, RefinementValue = "#" },
				new TaxonomyMenuItem {Text = "Profile", Url = Url.PageFaqDetails("faq-profile"), Count = 3, RefinementValue = "#" },
				new TaxonomyMenuItem {Text = "Orders", Url = Url.PageFaqDetails("faq-order"), Count = 3, RefinementValue = "#" },
				new TaxonomyMenuItem {Text = "Projects", Url = Url.PageFaqDetails("faq-project"), Count = 2, RefinementValue = "#" },
                new TaxonomyMenuItem {Text = "Products", Url = Url.PageFaqDetails("faq-product"), Count = 5, RefinementValue = "#" },
				new TaxonomyMenuItem {Text = "Companies", Url = Url.PageFaqDetails("faq-company"), Count = 8, RefinementValue = "#" },
				new TaxonomyMenuItem {Text = "Documents", Url = Url.PageFaqDetails("faq-document"), Count = 3, RefinementValue = "#" }
			};
			model.PageLinks.First().AddChild(faqSections);

			return View(model);
		}

		///// <summary>
		///// FAQs the specified unique identifier.
		///// </summary>
		///// <param name="id">The unique identifier.</param>
		///// <returns></returns>
		//public ActionResult Faq(int id)
		//{
		//	return Faq();
		//}



		private IEnumerable<TaxonomyMenuItem> GetPageLinks()
		{
			return new List<TaxonomyMenuItem> {
				new TaxonomyMenuItem { Text = "FAQs", Url = Url.PageFaq(), Key = "faq", IsRefinable = true },
				new TaxonomyMenuItem { Text = "Contact Us", Url = Url.PageContactUs(), Key = "contact" }
			};
		}

		internal IEnumerable<Breadcrumb> SetPageMetadata(string crumbText)
		{
			var list = new List<Breadcrumb>();

			list.Add(new Breadcrumb { Text = "Help", Url = Url.Action("Index") });

			if (crumbText != null)
				list.Add(new Breadcrumb() { Text = crumbText, Url = Url.Action(null) });

			SetPageMetadata(crumbText, list);

			return ViewBag.Breadcrumbs;
		}
	}
}