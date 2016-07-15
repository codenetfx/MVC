/// <reference path="../_references.js" />
/// <reference path="../../Lib/jquery/plugins/jquery.blockUI.js" />
/// <reference path="../_ULReferences.js" />

(function () {
	'use strict';
	UL.TaskDocumentDialog = function (elem, options) {
		/// <summary>
		/// 
		/// </summary>
		/// <param name="elem"></param>
		/// <param name="options"></param>
		///<field name="description" type="jQuery">The description field.</field>
		this.elem = elem;
		this.options = {};
		$.extend(this.options, options);
		this.form = null;
	};

	UL.TaskDocumentDialog.prototype = {
		init: function () {
			this.form = this.elem.find("form#" + this.options.formId);
			this.form.removeData('validator');
			this.form.removeData('unobtrusiveValidation');
			$.validator.unobtrusive.parse(this.form);

			if (this.form.find('#task-document').length > 0) {
				this.form.closest(".modal").modal("hide");
				var data = this.form.find('#task-document').data();
				if (data.canEdit.toLowerCase() === 'true') {
					window.open(data.documentUrl, 'Edit Document Online');
				}
				UL.Refresh();
			}

			this.initFormAjax();
		},
		initFormAjax: function () {
			var self = this;


			self.form.ajaxForm({
				replaceTarget: true,
				target: self.form.parent().parent(),
				beforeSubmit: function () {
					self.startBlocking();
					return $.proxy(UL.ValidateModal, self.form);
				},
				complete: function () {
					self.stopBlocking();
				},
				cache: false
			});
		},

		startBlocking: function () {
			var self = this;
			var blockOptions = {};
			blockOptions.css = {
				border: 'none',
				backgroundColor: 'transparent'
			};
			blockOptions.message = UL.loadAnimationLarge[0];
			blockOptions.baseZ = 1500;
			blockOptions.centerX = false;
			blockOptions.centerY = false;

			self.elem.block(blockOptions);
		},
	
		stopBlocking: function () {
			var self = this;
			self.elem.unblock();
		}
	};


	$.fn.taskDocumentDialog = function () {

		$(this).each(function (index, elem) {
			var $elem = $(elem);

			if ($elem.data("UL.TaskDocumentDialog") === undefined) {
				var data = $elem.data();
				var component = new UL.TaskDocumentDialog($elem, data);
				$elem.data("UL.TaskDocumentDialog", component);
				component.init();
			}

		});

		return $(this);

	};

}());