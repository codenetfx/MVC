/// <reference path="../_references.js" />
/// <reference path="../_ULReferences.js" />

(function () {
	'use strict';
	UL.ULInitAjaxForm = function (elem, options) {
		this.elem = elem;
		this.options = {};
		$.extend(this.options, options);
		this.form = null;
	};

	UL.ULInitAjaxForm.prototype = {
		init: function () {
			this.form = this.elem.find("form#" + this.options.formId);

			this.form.removeData('validator');
			this.form.removeData('unobtrusiveValidation');
			$.validator.unobtrusive.parse(this.form);

			this.initFormAjax();

			if (this.form.find('.modal-success-only').length > 0) {
				this.form.closest(".modal").modal("hide");
				UL.Refresh();
			}
		},
		initFormAjax: function () {
			var self = this;
			self.form.ajaxForm({
				replaceTarget: true,
				target: self.form.parent().parent(),
				beforeSubmit: function () {
					return $.proxy(UL.ValidateModal, self.form);
				},
				cache: false
			});

		}
		

	};

	$.fn.ulInitAjaxForm = function () {
		$(this).each(function (index, elem) {
			var $elem = $(elem);
			if ($elem.data('UL.ULInitAjaxForm') === undefined) {
				var data = $elem.data();
				var component = new UL.ULInitAjaxForm($elem, data);
				$elem.data('UL.ULInitAjaxForm', component);
				component.init();
			}
		});
		return $(this);
	};

}());