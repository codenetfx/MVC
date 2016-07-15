(function() {
	'use strict';
	UL.ProjectDialog = function(elem, options) {
		this.elem = elem;
		this.options = {};
		$.extend(this.options, options);
		this.form = null;
		this.projectTemplate = null;
		this.projectTemplates = null;
		this.overrideAutoComplete = null;
	};
	UL.ProjectDialog.prototype = {
		init: function() {
			this.form = this.elem.find("form#" + this.options.formId);
			this.projectTemplate = this.elem.find('#ProjectTemplate');
			this.overrideAutoComplete = this.elem.find('#override-auto-complete');
			var datalist = this.elem.find('#project-templates-json');
			this.projectTemplates = datalist.data('json');
			this.form.removeData('validator');
			this.form.removeData('unobtrusiveValidation');
			$.validator.unobtrusive.parse(this.form);
			this.projectTemplate.on("change", this.projectTemplateChangeHandler());
		    this.projectTemplate.trigger("change");
			this.initFormAjax();
		},
		initFormAjax: function () {
			var self = this;
			self.form.ajaxForm({
				replaceTarget: true,
				target: self.form.parent().parent(),
				beforeSubmit: function () {
					return $.proxy(UL.ValidateModal, self.form);
				},
				cache: false,
				success: function (data, status, jqXHR) {
				    var location = jqXHR.getResponseHeader("location");

					if (location !== undefined && location !== null) {
						window.location = jqXHR.getResponseHeader("location");
					}
				},
				error: function (jqXHR, textStatus, errorThrown) {
					var title = textStatus || "Error";
					var body = errorThrown || "An unexpected error has occurred.";
					try {
						var errorPage = $(jqXHR.responseText.trim());
						title = errorPage.find("h1").text() || title;
						body = errorPage.find(".body-content").html() || errorPage.find("h2").text() || body;

					} catch (ignore) { }

					self.elem.closest(".modal").modal("loading");
					self.elem.find(".modal-body").html(body);
					self.elem.closest(".modal").modal('removeLoading');
					$.unblockUI();
				}

			});
		},
		projectTemplateChangeHandler : function() {
			var self = this;
			return function () {
				var currentVal = $(this).val();
				var item = Enumerable.From(self.projectTemplates)
					.FirstOrDefault(null, function (x) {
						return x.Id === currentVal;
					});

				if (item !== null && item !== undefined && item.AutoCompleteProject === true) {
					self.overrideAutoComplete.show();
				}
				else {
					self.overrideAutoComplete.hide();
				}
			};
		}
	};

	$.fn.projectDialog = function () {
		$(this).each(function (index, elem) {
			var $elem = $(elem);
			if ($elem.data("UL.ProjectDialog") === undefined) {
				var data = $elem.data();
				var component = new UL.ProjectDialog($elem, data);
				$elem.data("UL.ProjectDialog", component);
				component.init();
			}

		});
		return $(this);
	};

}());