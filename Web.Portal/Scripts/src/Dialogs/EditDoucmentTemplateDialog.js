/// <reference path="../_references.js" />
/// <reference path="../Widgets/UL.Proxy.js" />


(function () {
	"use strict";
	$.fn.editDocumentTemplateDialog = function () {

		$(this).each(function (index, element) {
			var elem = $(element);
			var data = elem.data();
			var form = elem.find("form#" + data.formId);
			var fileInput = form.find('input[type="file"]');

			form.ajaxForm({
				replaceTarget: true,
				target: form.parent().parent(),
				success: function (result) {
					$(this).closest(".modal").modal("hide");
					window.location.reload();
				},
				cache: false
			});

			var businessUnitCheckHandler = function() {
				if ($(".chkBusinessUnit input:checkbox:checked").length > 0) {
					$("label[for='BusinessUnitsValidation']").hide();
				} 
			};

			elem.find(".chkBusinessUnit input:checkbox").on("click", businessUnitCheckHandler);
			
			var displayErrors = function (validator) {
				var container = form.find("[data-valmsg-summary=true]"),
				    list = container.find("ul");

				if (list && list.length && validator.errorList.length) {
					list.empty();
					container.addClass("validation-summary-errors")
					    .removeClass("validation-summary-valid");

					$.each(validator.errorList, function () {
						$("<li />").html(this.message).appendTo(list);
					});
				}
			};

			var submitClickHandler = function (e, ui) {

				e.stopImmediatePropagation();

				var validator = form.validate();
				var proxyOptions = new UL.ProxyOptions();
				proxyOptions.blockedElement = elem;
				var proxy = new UL.Proxy(proxyOptions);
			
				proxy.send({ documentTemplate: form.serializeObject() }, data.validationUrl,
					function (successful, vdata, error) {

						var options = {};
						if (data.mode === "create" && fileInput.val() ==='') {
							options.File = "File is required.";
							validator.showErrors(options);
							displayErrors(validator);
						}
						if (!vdata.Successful) {
							Enumerable.From(vdata.Data).ForEach(function(item) {
								options[item.Key] = item.ErrorMessage;
								validator.showErrors(options);
							});
							displayErrors(validator);
							
						} else {
							
							var fileName = fileInput.val();
							if (fileName === '') {
								//Edit for No file to save
								if (data.mode === "edit") {
									fileInput.prop("disabled", "disabled");
									form.submit();
								} 
							} else {
								if (data.mode === "edit") {
									form.attr("action", data.formAction);
								}
								UL.FancyFormSubmit(e);
							}
						}
					});

				
				return false;
			};

			form.find(".btn-primary").on("click", submitClickHandler);

			//form.submit(function (e, ui) {

			//});

			fileInput.fancyUpload({
				dropHole: ".file-drag"
			});

		});

		return $(this);
	};
}());


$.fn.serializeObject = function () {
	'use strict';
	var o = {};
	var a = this.serializeArray();
	$.each(a, function () {
		if (o[this.name] !== undefined) {
			if (this.value.toString() !== 'true' && this.value.toString() !== 'false') {
				if (!o[this.name].push) {
					o[this.name] = [o[this.name]];
				}
				o[this.name].push(this.value || '');
			}
			else if (this.value.toString() === 'true') {
				o[this.name] = this.value;
			}
		} else {
			o[this.name] = this.value || '';
		}
	});
	return o;
};


