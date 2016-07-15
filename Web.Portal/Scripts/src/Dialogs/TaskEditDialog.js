(function () {
	'use strict';

	UL.TaskEditDialog = function (elem, options) {
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
		this.taskOwner = null;
		this.description = null;
		this.projectHandler = null;
		this.taskOwnerAssigned = null;
		this.ProjectHandlerToken = null;
		this.estimatedDuration = null;
		this.predecessorTask = null;
		this.startDate = null;
		this.readonlyFields = null;
	};

	UL.TaskEditDialog.prototype = {
		init: function () {
			this.form = this.elem.find("form#" + this.options.formId);
			this.taskOwner = this.elem.find('#TaskOwner');
			this.taskName = this.elem.find('.ul-combobox');
			this.description = this.elem.find('#Description');
			this.projectHandler = this.elem.find('#ProjectHandler');
			this.taskOwnerAssigned = this.elem.find("input[name='TaskOwnerAssigned']");
			this.ProjectHandlerToken = this.elem.find('#ProjectHandlerToken');
			this.estimatedDuration = this.elem.find('input[name="EstimatedDuration"]');
			this.submitButton = this.elem.find('.task-submit');
			this.addNotification = this.elem.find('.notification-add');
			this.removeNotification = this.elem.find('.notification-remove');
			this.notifications = this.elem.find('.task-notifications input');
			this.readonlyFields = this.elem.find('#read-only-fields').data('json');
			this.submitButton.on('click', this.getBillingTaskTriggerHander());
			// this.submitButton.on('click', this.getUpdateTaskHandler());
			this.taskName.combobox("registerSelectEventHandler", this.getComboBoxSelectHandler());

			this.predecessorTask = this.elem.find('input[name="PredecessorTask"]');
			this.startDate = this.elem.find('input[name="StartDate"]');

			this.taskTypeList = this.elem.find('#taskTypesList').data("json");
			this.businessUnitDropdown = this.elem.find('.business-unit-dropdown');

			this.predecessorTask.on('blur', this.predecessorTaskBlurHandler());
			this.addNotification.on('click', this.getAddNotificationHandler());
			this.elem.on('click', '.notification-remove', this.getRemoveNotificationHandler());

			if (this.options.mode === 'edit' && this.options.hasActivePredecessor && this.options.hasActivePredecessor.toLowerCase() === 'true') {
				this.predecessorTaskHandler();
			}

			this.initFormAjax();

			this.applyReadonly();
			this.blockTaskOwner();
		    this.blockNotificationSection();
			if (this.options.isReactiveRequest && this.options.isReactiveRequest.toLowerCase() === 'true') {
				this.form.closest(".modal").modal('layout');
				this.form.closest(".modal").on('hidden', function () { UL.Refresh(); });
			}
			if (this.taskOwner.length > 0 && this.taskOwner.data().blockTaskOwner === true) {
				this.taskOwner.find('input').attr('disabled', true);
			}

			this.businessUnitDropdown.on('change', this.businessUnitChangeHandler());
			this.businessUnitDropdown.trigger("change");
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

		},
		getComboBoxSelectHandler: function () {
			var self = this;
			return function (event, item) {

				if (item) {
					self.description.val(item.Description);
					self.description.attr("readonly", "readonly");
					self.estimatedDuration.val(item.EstimatedDuration);

					if (item.TaskOwner === self.ProjectHandlerToken.val()) {
						self.taskOwner.val(self.projectHandler.val());
					}
					else {
						self.taskOwner.val(item.TaskOwner);
					}

					self.taskOwnerAssigned.filter("[value='" + item.TaskOwnerAssignedString + "']").prop("checked", true);

					//Don't Delete it. We might need in future.
					//Apply Readonly Fields Not Project handlers 
					//if (self.options.currentUser !== self.options.projectHandler) {
					//	self.enableReadonlyFields();
					//	var proxy = new UL.Proxy();
					//	proxy.send({ id: item.Id }, self.options.taskBehaviorUrl,
					//		function(successful, vdata, error) {
					//			self.readonlyFields = vdata.readonlyFields;
					//			self.applyReadonly();
					//		});
					//}

				} else {
					if (self.description.val().length === 0) {
						self.description.val('');
					}
					self.description.removeAttr("readonly");
					self.estimatedDuration.val('');
					self.taskOwner.val('');
					self.taskOwnerAssigned.last().prop("checked", true);

				}
			};
		},
		getBillingTaskTriggerHander: function () {
			var self = this;
			return function (e, ui) {
				var phase = self.elem.find('[name="Phase"]');
				var isBillingTriggerTask = self.options.isBillingTriggerTask;

				if (isBillingTriggerTask && isBillingTriggerTask.toLowerCase() === 'true' &&
					(phase.val() === 'Completed' || phase.val() === 'Canceled')) {

					var options = new UL.ModalOptions();
					var callback = function (arg) {
						if (arg.result === options.submitButtonText) {
							arg.modal.modal('hide');
							if (self.getUpdateTaskHandler() === true) {
								self.form.submit();
							}
						}
					};

					if (phase.val() === 'Canceled') {
						options.submitButtonText = "Cancel Task";
						options.message = self.options.billingTriggerCancelMessage;
						options.title = "Cancel Fulfillment Complete Trigger Task";
						options.callback = callback;
						UL.ShowModal(options);
						return false;
					}

					if (phase.val() === 'Completed' && self.options.billingTriggerCompleteMessage.length > 0) {
						options.submitButtonText = "Complete Task";
						options.message = self.options.billingTriggerCompleteMessage;
						options.title = "Complete Fulfillment Complete Trigger Task";
						options.callback = callback;
						UL.ShowModal(options);
						return false;
					}

				}
				return self.getUpdateTaskHandler();

			};
		},

		getUpdateTaskHandler: function () {
			var self = this;
			var assingedVal = self.form.find('[name="TaskOwnerAssigned"]:checked');
			var phase = self.elem.find('[name="Phase"]');

			if ((assingedVal.val() !== 'AssignToMe' && self.options.currentUser.toLowerCase() !== self.options.projectHandler.toLowerCase())
				&& (phase.val() === 'Completed' || phase.val() === 'Canceled')) {
				var bodyMesssage;
				if (phase.val() === 'Canceled') {
					bodyMesssage = "You are not the Owner of this Project or Task. Are you sure you want to Cancel this Task?";
				} else {
					bodyMesssage = "You are not the Owner of this Project or Task. Are you sure you want to Complete this Task?";
				}
				UL.ShowConfirm("Warning", bodyMesssage, function (arg) {
					if (arg.result === 'Yes') {
						arg.modal.modal('hide');
						self.form.submit();
					}
				});

				return false;
			}
			return true;
		},

		predecessorTaskBlurHandler: function () {
			var self = this;

			return function () {
				self.predecessorTaskHandler();
			};
		},

		predecessorTaskHandler: function () {
			var self = this;

			if (!self.predecessorTask.attr('readonly')) {

				if (self.predecessorTask.val()) {
					self.startDate.val('');
					self.startDate.parent().find('.add-on').hide();
					self.startDate.attr('disabled', 'disabled');

				} else {
					self.startDate.removeAttr('disabled');
					self.startDate.parent().find('.add-on').show();
				}
			}
		},

		getAddNotificationHandler: function () {
			var self = this;
			return function (e, ui) {
				var $table = self.elem.find('div.task-notifications > table');
				var row = $table.find('tr').length;
				var tableRow = self.elem.find('#newTaskNotificationRow tbody tr');
				var index = parseInt(row, 10);
				var newRow = tableRow.htmlAll().replace(/newTaskNotificationRow/g, index);
				$table.find('tbody').append(newRow);
				$table.find('[name = "Notifications[' + index + '].Email"]').ulAutoComplete();
			};
		},

		getRemoveNotificationHandler: function () {
			var self = this;
			return function (e, ui) {
				$(e.target).closest('tr').remove();
				self.elem.find('div.task-notifications > table tr').each(function (index) {
					var prefix1 = "Notifications[" + index + "]";
					var prefix2 = "Notifications_" + index;
					$(this).find("input").each(function () {
						this.id = this.id.replace(/Notifications_\d+/, prefix2);
						this.name = this.name.replace(/Notifications\[\d+\]/, prefix1);
					});
				});
			};
		},

		applyReadonly: function () {
			var self = this;
			Enumerable.From(self.readonlyFields).ForEach(function (item) {
				var inputFeild = self.elem.find('input[name="' + item.FieldName + '"]');
				inputFeild.each(function () {
					if ($(this).attr('type') === 'radio' && !$(this).is(':checked')) {
						$(this).attr("disabled", true);
					} else {
						$(this).attr("readonly", "readonly");
					}
				});
				inputFeild.attr('title', item.TitleDesc);
				var parentdiv = inputFeild.parent('.date');
				if (parentdiv.length > 0) {
					parentdiv.find('.add-on').hide();
					inputFeild.datepicker('remove');
				}
			});
		},
		enableReadonlyFields: function () {
			var self = this;
			Enumerable.From(self.readonlyFields).ForEach(function (item) {
				var inputFeild = self.elem.find('input[name="' + item.FieldName + '"]');
				inputFeild.each(function () {
					if ($(this).attr('type') === 'radio' && !$(this).is(':checked')) {
						$(this).removeAttr("disabled");
					} else {
						$(this).removeAttr("readonly");
					}
				});
				inputFeild.removeAttr('title');
				var parentdiv = inputFeild.parent('.date');
				if (parentdiv.length > 0) {
					parentdiv.find('.add-on').show();
					inputFeild.datepicker('update');
				}
			});
		},
		blockTaskOwner: function () {
			var self = this;
			var taskOwner = self.elem.find("#task-owner");
			taskOwner.find(".task-restricted").hide();
			if (taskOwner.data("blockTaskOwner")) {
				taskOwner.find("*").each(function() {
					if ($(this).attr('type') === 'radio' && !$(this).is(':checked')) {
						$(this).attr("disabled", true);
					} else {
						$(this).attr("readonly", "readonly");
					}
				});
				taskOwner.find("*").prop("title", taskOwner.data("titleText"));
				taskOwner.find(".task-restricted").show();
			}
		},
		blockNotificationSection: function () {
		    var assigned = this.form.find('[name="TaskOwnerAssigned"]:checked');
		    if (assigned.val() !== 'AssignToMe'
                && this.options.currentUser.toLowerCase() !== this.options.projectHandler.toLowerCase()) {

	            this.addNotification.attr("disabled", true);
	            this.removeNotification.attr("disabled", true);
	            this.notifications.attr("readonly", "readonly");

	        }
	    },
		businessUnitChangeHandler: function () {
			var self = this;
			return function () {
			    var selectedBusinessUnit = self.businessUnitDropdown.find("option:selected").text();
			    var filteredTaskTypes = [];
			    if (selectedBusinessUnit.length !== 0) {
			        if (selectedBusinessUnit.toLowerCase() === "all") {
			            filteredTaskTypes = Enumerable.From(self.taskTypeList).ToArray();
			        }
			        else {
			            filteredTaskTypes = Enumerable.From(self.taskTypeList).Where(function(tt) {
			                var businessUnits = Enumerable.From(tt.BusinessUnits).Select(function(bu) {
			                    return bu.Code.toLowerCase();
			                }).ToArray();
			                return Enumerable.From(businessUnits).Contains(selectedBusinessUnit.toLowerCase())
			                    || Enumerable.From(businessUnits).Contains("all");
			            }).ToArray();
			        }
			    }
				self.taskName.combobox("setList", filteredTaskTypes);
			};
		}
	};

	$.fn.taskEditDialog = function () {

		$(this).each(function (index, elem) {
			var $elem = $(elem);

			if ($elem.data("UL.TaskEditDialog") === undefined) {
				var data = $elem.data();
				var component = new UL.TaskEditDialog($elem, data);
				$elem.data("UL.TaskEditDialog", component);
				component.init();
			}

		});

		return $(this);

	};
}());