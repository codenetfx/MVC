(function () {
	"use strict";
	$.fn.editTaskTypeDialog = function () {

		$(this).each(function (oindex, $elem) {
			var elem = $($elem);
			var options = elem.data();
			if (!options.isInit) {
				elem.data('isInit', true);
				var addDocumentTemplateButton = elem.find(options.addDocumentTemplateButtonId);
				var selectionsContainer = elem.find('table.document-templates tbody');
				var documentTemplateDropdownList = elem.find(options.documentTemplateDropdownListId);
				var newrowTxt = function (index, id, name) {
					var tablerow = elem.find(options.newDocumentTemplateRowId).find('tbody').html();
					return tablerow.replace(/newrow/g, index).replace(/#id#/g, id).replace(/#name#/g, name);
				};
				var form = elem.find('form');

				documentTemplateDropdownList.prop('selectedIndex', 0);
				addDocumentTemplateButton.on("click", function (e) {
					e.preventDefault();

					var si = $('#documentTemplateDropdownList')[0].selectedIndex;
					if (si === 0) {
						return;
					}

					if (selectionsContainer.find('input.document-template-id[value=\"' + documentTemplateDropdownList.val() + '\"]').length !== 0) {
						return;
					}

					var selectedDocumentTemplateRows = selectionsContainer.find('tr');
					var newRow = newrowTxt(selectedDocumentTemplateRows.length, documentTemplateDropdownList.val(), documentTemplateDropdownList.children(':selected').text());
					var newRowObject = $.parseHTML(newRow);
					selectionsContainer.append(newRowObject);
				});

				selectionsContainer.on('click', "button.remove-document-template", function (e) {
					var row = $(this).closest('tr');
					row.remove();
					selectionsContainer.find('tr').each(function (index) {
						var prefix1 = "DocumentTemplates[" + index + "]";
						var prefix2 = "DocumentTemplates_" + index;
						$(this).find("input").each(function () {
							this.id = this.id.replace(/DocumentTemplates_\d+/, prefix2);
							this.name = this.name.replace(/DocumentTemplates\[\d+\]/, prefix1);
						});
					});
				});

				//hook ajax form submissions
				form.ajaxForm({
					replaceTarget: true,
					target: elem.parent(),
					beforeSubmit: $.proxy(UL.ValidateModal, form),
					cache: false
				});

				elem.parent().parent().find('.loading-mask').remove();

				var taskTextbox = elem.find('input[name="TaskOwner"]');
				var taskCheckbox = elem.find('input:checkbox[name=UseProjectHandler]');

				taskCheckbox.on("change", function () {
					if ($(this).is(":checked")) {
						taskTextbox.val('');
					}
				});

				taskTextbox.on("keyup", function () {
					if ($(this).val().length > 0 && taskCheckbox.is(':checked')) {
						taskCheckbox.uncheck();
					}
				});

				taskTextbox.on("blur", function () {
					var taskOwnerText = $(this).val();
					taskOwnerText = taskOwnerText.trim();
					$(this).val(taskOwnerText);
				});

				var addNotification = elem.find('.notification-add');
				addNotification.on('click', function () {
					var $table = elem.find('div.task-notifications > table');
					var row = $table.find('tr').length;
					var tableRow = elem.find('#newTaskTypeNotificationRow tbody tr');
					var index = parseInt(row, 10);
					var newRow = tableRow.htmlAll().replace(/newTaskTypeNotificationRow/g, index);
					$table.find('tbody').append(newRow);
					$table.find('[name = "Notifications[' + index + '].Email"]').ulAutoComplete();
				});

				elem.on('click', '.notification-remove', function (e) {
					$(e.target).closest('tr').remove();
					elem.find('div.task-notifications > table tr').each(function (index) {
						var prefix1 = "Notifications[" + index + "]";
						var prefix2 = "Notifications_" + index;
						$(this).find("input").each(function () {
							this.id = this.id.replace(/Notifications_\d+/, prefix2);
							this.name = this.name.replace(/Notifications\[\d+\]/, prefix1);
						});
					});
				});
			}
		});
		return $(this);
	};
}());
