/// <reference path="../_references.js" />
/// <reference path="../_ULReferences.js" />

(function () {
    "use strict";
    if (!window.UL) {
        window.UL = {};
    }
    UL.ProjectTemplateDialog = function(element) {
        this.element = element;
        this.sourceList = this.element.find("#taskTypesList").data("json");
    };
    UL.ProjectTemplateDialog.prototype =
    {
        filterTaskTypes: function(businessUnit) {
            /// <summary>
            /// Filters the combobox dropdowns to display only those task types that match the specified business unit.
            /// </summary>
            /// <param name="businessUnit" type="String"></param>

            var filteredList = [];
            if (businessUnit.length === 0) {
                return filteredList;
            }

            if (businessUnit.toLowerCase() === "all") {
                filteredList = Enumerable.From(this.sourceList).ToArray();
            } else {
                filteredList = Enumerable.From(this.sourceList).Where(function (tt) {
                    var businessUnits = Enumerable.From(tt.BusinessUnits).Select(function (bu) {
                        return bu.Code.toLowerCase();
                    }).ToArray();
                    return Enumerable.From(businessUnits).Contains(businessUnit.toLowerCase())
                        || Enumerable.From(businessUnits).Contains("all");
                }).ToArray();
            }

            return filteredList;
        }
    };
    $.fn.projectTemplateDailog = function () {
		$(this).each(function (index, element) {

			var elem = $(element);
			var data = elem.data();
			var form = elem.find("form#" + data.formId);

			if (data.init === undefined) {
			    var dialog = new UL.ProjectTemplateDialog(elem);
				elem.data("init", true);
				var validate = function() {
					return $.proxy(UL.ValidateModal, form);
				};
				form.closest(".modal").modal({ backdrop: 'static', keyboard: false }).addClass("modal-project-template");
				form.ajaxForm({
					replaceTarget: true,
					target: form.parent().parent(),
					beforeSubmit: validate,
					cache: false
				});

				var businessUnitCheckboxHandler = function (e, ui) {
					if ($(this).is(':checked')) {
						if ($(this).parent('label').text().toLowerCase() === "all") {
							$('.check').each(function(ind, checkbox) {
								$(checkbox).prop('checked', false);
							});
						} else {
							$('.check').each(function(ind, checkbox) {
								if ($(checkbox).parent('label').text().toLowerCase() === "all") {
									$(checkbox).prop('checked', false);
								}
							});
						}
						$(this).prop('checked', true);
					}
				};
				elem.find('.check').on('click', businessUnitCheckboxHandler);

				var newrowTxt = function(rowindex) {
					var tablerow = elem.find('#idnewrow tbody tr');
					var txt = tablerow.htmlAll().replace(/newrow/g, rowindex);
					return $(txt);
				};
				var deleteTask = function (e, link) {
					e.preventDefault();
					$(this).parents('tr').remove();
				};
				var addTaskHandler = function(e, ui) {
					e.preventDefault();
					var $table = $(this).parent().children().find('table.template-tasks');
					var row = $table.find('tr:last').find('td:first').find('input:first').val();
					if (!row) {
						row = 0;
					}
					var newrow = newrowTxt(parseInt(row, 10) + 1);
					$table.find('tbody').append(newrow);

					newrow.find(".ul-combobox").combobox().combobox("registerSelectEventHandler", function (event, item) {
						var descTxtbox = newrow.find('.task-description');
						if (item) {
							descTxtbox.val(item.Description);
							descTxtbox.attr("readonly", "true");
						} else {
							descTxtbox.val('');
							descTxtbox.removeAttr("readonly");
						}
					});
					newrow.find("a.deleteTask").on("click", deleteTask);

					newrow.find(".business-unit-dropdown").change(function () {
					    var selectedText = $(this).find("option:selected").text();
					    var filteredTaskTypes = dialog.filterTaskTypes(selectedText);
					    $(this).closest("tr").find(".ul-combobox").combobox().combobox("setList", filteredTaskTypes);
					});
					newrow.find(".business-unit-dropdown").trigger("change");

				};
				//elem.find(".addTask").off("click", addTaskHandler);
				elem.find(".addTask").on("click", addTaskHandler);

				elem.find('.deleteTask').on('click', deleteTask);

			    elem.find(".ul-combobox").each(function() {
			    	$(this).combobox("registerSelectEventHandler", function (event, item) {
			    		var descTxtbox = $(this).parent().parent().find('.task-description');
			    		if (item) {
			    			descTxtbox.val(item.Description);
			    			descTxtbox.attr("readonly", "true");
			    		} else {
			    			descTxtbox.val('');
			    			descTxtbox.removeAttr("readonly");
			    		}
			    	});
			    });

			    elem.find(".ul-combobox").each(function () {
			        var businessUnitCode = $(this).closest("td").prev().text();
			        $(this).combobox("setList", dialog.filterTaskTypes(businessUnitCode));
			    });

			    elem.find(".business-unit-dropdown").change(function () {
			        var selectedText = $(this).find("option:selected").text();
			        var filteredTaskTypes = dialog.filterTaskTypes(selectedText);
			        $(this).closest("tr").find(".ul-combobox").combobox().combobox("setList", filteredTaskTypes);
			    });
			    elem.find(".business-unit-dropdown").trigger("change");
			}
			return $(this);
		});
	};
}());