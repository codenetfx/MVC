/// <reference path="../_references.js" />
/// <reference path="../_ULReferences.js" />
/// <reference path="UL.MvcJqGrid.js" />
/// <reference path="../Dialogs/AssignDialog.js" />

(function () {
    'use strict';
    UL.TaskGrid = function (grid) {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="grid"></param>
        ///<field name="assignDialog" type="UL.AssignDialog"></field>

        //Grid Instance
        this.MvcJqGrid = grid || UL.MvcJqGrid;
        this.gridRegistration = null;

        //dialog references
        this.assignDialog = null;

        // event handlers keys
        this.taskStatusChangeHandlerKey = "UL.TaskGrid.TaskStatusChanged";
        this.startDataChangedHandlerKey = "UL.TaskGrid.StartDateChanged";
        this.taskOwnerChangedHandlerKey = "UL.TaskGrid.TaskOwnerChanged";

        // Dat Rules keys
        this.softWarningDataRuleKey = "UL.TaskGrid.SoftWarningRule";
        this.readonlyColumnDataRuleKey = "UL.TaskGrid.ReadonlyColumnRule";
        this.beforeEditDataRuleKey = "UL.TaskGrid.BeforeRowEditRule";
        this.multiModeOnRowSelectedRuleKey = "UL.TaskGrid.OnRowSelectedRule";
        this.beforeDeleteRuleKey = "UL.TaskGrid.BeforeDeleteEditRule";

        //Column Names
        this.reminderDateColumn = "ReminderDate";
        this.shortDiscriptionColumn = "ShortDescription";
        this.startDateColumn = "StartDate";
        this.taskNameColumn = "TaskName";
        this.taskOwnerColumn = "TaskOwner";
        this.taskStatusColumn = "TaskStatus";
        this.isPredefinedTaskColumn = "IsPredefinedTask";
        this.projectHandlerColumn = "ProjectHandler";
        this.currentUsesrColumn = "CurrentUser";
        this.taskNumberColumn = "TaskNumber";
        this.dueDateColumn = "EndDate";

        //Messages
        this.taskStatusWarning = "You are not the Owner of this Project or Task(s). Are you sure you want to Complete/Cancel the following task(s)?";
        this.billingTriggerAnnotation = "Fulfillment Complete Trigger Task";

        // typed for readability.  Need this object back from the server
        this.taskStatusList = {
            NotScheduled: { value: "NotScheduled", display: "Not Scheduled" },
            NotStarted: { value: "NotStarted", display: "Not Started" },
            InProgress: { value: "InProgress", display: "In Progress" },
            OnHold: { value: "OnHold", display: "On Hold" },
            Completed: { value: "Completed", display: "Completed" },
            Canceled: { value: "Canceled", display: "Canceled" },
            RemoveHold: { value: "RemoveHold", display: "Remove Hold" },
            AwaitingAssignment: { value: "AwaitingAssignment", display: "Awaiting Assignment" }
        };
    };

    //Custom column Renderers keys
    UL.TaskGrid.BillingTriggerAnnotation = "UL.TaskGrid.BillingTriggerAnnotation";

    UL.TaskGrid.prototype = {
        init: function () {
            /// <summary>
            /// 
            /// </summary> 

            this.MvcJqGrid.RegisterEditorEventHandler(this.taskStatusChangeHandlerKey, "change", this.getTaskStatusSelectHandler());
            this.MvcJqGrid.RegisterEditorEventHandler(this.startDataChangedHandlerKey, "change", this.getStartDateChangeHandler());
            this.MvcJqGrid.RegisterEditorEventHandler(this.taskOwnerChangedHandlerKey, "blur", this.getTaskOwnerChangedHandler());
            this.MvcJqGrid.RegisterEditorColumnRule(this.readonlyColumnDataRuleKey, this.getApplyColumnReadonlyRules());
            this.MvcJqGrid.RegisterEditorColumnRule(this.softWarningDataRuleKey, this.getApplySoftwarningRules());
            this.MvcJqGrid.RegisterEditorColumnRule(this.beforeEditDataRuleKey, this.getApplyBeforeRowEditRules());
            this.MvcJqGrid.RegisterEditorColumnRule(this.multiModeOnRowSelectedRuleKey, this.getMultiModeOnRowSelectedRule());
            this.MvcJqGrid.RegisterEditorColumnRule(this.beforeDeleteRuleKey, this.getBeforeDeletedRule());

            //temporary, taskGrid shouldn't always init, remove when fixed.
            if ($("#EditableTaskGrid").length > 0) {

                setTimeout(function () {
                    $('a[data-toggle="modal"][data-target="#DeleteModal"]').off("click");
                    $('#link-taskCompleteModal').off("click");
                    $('.assign-multi-btn').attr("data-target", ".assign-modal");
                    $('.assign-multi-btn').attr("href", "#");
                }, 500);
            }
        },
        getBeforeDeletedRule: function () {
            var self = this;

            return function (row, columnDictionary, selectedArgs) {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="row" type="UL.MvcJqGrid.GridRow"></param>
                /// <param name="columnDictionary"></param>
                /// <param name="selectedArgs"></param>

                var deferred = $.Deferred();
                var menuDeleteButton = $(".selection-pane a.group-delete");
                var modalData = menuDeleteButton.data();
                var rowData = row.rowData();
                var statusValue = rowData.TaskStatus.SelectedValue;
                var taskClosed = (statusValue === "Completed" || statusValue === "Canceled");

                if (modalData && !taskClosed) {
                    $(modalData.target).modal('show');
                    var handler = self.getDeleteMultiClickHandler();

                    var args = {
                        deferred: deferred,
                        selectedArgs: selectedArgs
                    };

                    handler.apply(menuDeleteButton, [{ data: args }]);
                }
                else {

                    var alertCallback = function () {
                        deferred.resolve({
                            result: false,
                            action: self.MvcJqGrid.MultiMenuActions.Delete
                        });
                    };
                    if (!modalData) {
                        UL.ShowAlert("Delete Operation Restricted", "Only the Project Handler is allowed to delete tasks for this project.", alertCallback);
                    }
                    else if(taskClosed) {
                        UL.ShowAlert("Delete Operation Restricted", "Tasks that are Completed or Canceled cannot be deleted.", alertCallback);
                    }
                    else {
                        // this is a safety net.
                        setTimeout(alertCallback, 20);
                    }
                }
                return deferred;
            };
        },
        getMultiModeOnRowSelectedRule: function () {
            var self = this;

            return function (row, columnDictionary, selectedArgs) {
                /// <summary>
                /// 
                /// </summary>
                /// <param name="row" type="UL.MvcJqGrid.GridRow"></param>
                /// <param name="columnDictionary"></param>
                /// <param name="selectedArgs"></param>

                var deferred = $.Deferred();

                var menu = $(".selection-pane");
                var menuDeleteButton = $(".selection-pane a.group-delete");
                var menuCompleteButton = $(".selection-pane #link-taskCompleteModal");
                // **** May want to refactor how the links are built instead of the removing attributes.
                // **** this is required to utilize the single save action to trigger softwarnings correctly.
                menuCompleteButton.removeAttr("data-target");
                menuCompleteButton.removeAttr("data-toggle");
                var menuAssignButton = $(".assign-multi-btn");
                self.assignDialog = $(menuAssignButton.data().target).data("UL.AssignDialog");
                var countLabel = $(".selection-pane .selected-count");

                countLabel.html(selectedArgs.selectedRows.length);
                var rowData = row.rowData();
                var statusValue = rowData.TaskStatus.SelectedValue;
                if (statusValue !== "Canceled" && statusValue !== "Completed") {

                    if (selectedArgs.selectedRows.length > 0) {
                        menu.show();
                        var args = {
                            deferred: deferred,
                            selectedArgs: selectedArgs
                        };

                        menuDeleteButton.off("click").on("click", null, args, self.getDeleteMultiClickHandler());
                        menuCompleteButton.off("click").on("click", null, args, self.getCompleteMultiClickHandler());
                        menuAssignButton.off("click").on("click", null, args, self.getAssignMultiClickHandler());
                    }
                    else {
                        menu.hide();
                        menuDeleteButton.off("click");
                        menuCompleteButton.off("click");
                        menuAssignButton.off("click");
                    }
                }
                else {

                    
                    setTimeout(function () {
                        row.unselect();
                        deferred.resolve({ result: false });
                    }, 10);              
                }

                return deferred;

            };
        },
        getDeleteMultiClickHandler: function () {
            /// <summary>
            /// TaskCompleteModal
            /// </summary>

            var self = this;

            return function (e) {
                var data = e.data;
                var modalData = $(this).data();

                if (modalData.modalTitle) {
                    $('#DeleteConfirmModalLabel').html(modalData.modalTitle);
                }
                var deleteWarningMsg = "Are you sure you want to delete the selected task(s)? <br/>If any parent tasks are selected, ";
                deleteWarningMsg += "then all child tasks associated with them will also be deleted and you will not be able to recover this information.";
                var rows = [];
                Enumerable.From(data.selectedArgs.selectedRows).ForEach(function (row) {
                    rows.push(row.rowData());
                });

                var message = self.buildMultiSelectDeleteWarning(deleteWarningMsg, rows);
                $('#delete_modal_body').html("").append(message);

                $('button#submitDelete').off("click").on("click", function (e, ui) {
                    e.preventDefault();

                    data.deferred.resolve({
                        result: true,
                        action: self.MvcJqGrid.MultiMenuActions.Delete,
                        rowData: Enumerable.From(data.selectedArgs.selectedRows)
                            .Select(function (x) { return x.rowData(); }).ToArray()
                    });

                    $(this).closest(".modal").modal('hide');
                });
            };
        },
        getCompleteMultiClickHandler: function () {
            /// <summary>
            /// 
            /// </summary>

            var self = this;

            return function (e) {
                var data = e.data;
                var rowDataList = [];
                Enumerable.From(data.selectedArgs.selectedRows).ForEach(function (row) {
                    var rowData = row.rowData();
                    rowData.TaskStatus.SelectedValue = "Completed";
                    rowData.TaskStatus.SelectedDisplay = "Completed";
                    rowDataList.push(rowData);
                });

                self.applySoftwarnings(rowDataList).done(function (result) {
                    if (result) {
                        data.deferred.resolve({
                            action: self.MvcJqGrid.MultiMenuActions.Update,
                            rowData: rowDataList
                        });
                    }
                });
            };
        },
        updateTasksProperty: function (rows, field, value) {
            var rowdata = [];

            Enumerable.From(rows).ForEach(function (x) {
                var data = x.rowData();
                data[field] = value;
                rowdata.push(data);
            });
            return rowdata;
        },
        getAssignMultiClickHandler: function () {
            var self = this;
            return function (e) {
                var data = e.data;
                self.assignDialog.continueBtn.off("click").on("click", null, data,
                        self.getAssignDialogContinueButtonClickHandler());
            };
        },
        getAssignDialogContinueButtonClickHandler: function () {

            var self = this;

            return function (e) {
                var assignWarningModal = $(".assign-warning");
                assignWarningModal.find(".continue-btn").off("click")
                    .on("click", null, e.data, self.getAssignWarningContinueButtonClickHandler());
                assignWarningModal.modal();

            };
        },
        getAssignWarningContinueButtonClickHandler: function () {
            var self = this;

            return function (e) {

                var data = e.data;
                var newValue = self.assignDialog.getSelectTaskOwner();
                var updatedRowData = self.updateTasksProperty(data.selectedArgs.selectedRows, "TaskOwner", newValue);
                data.deferred.resolve({
                    action: self.MvcJqGrid.MultiMenuActions.Update,
                    rowData: updatedRowData
                });

                self.assignDialog.close();
                $(this).closest('.modal').modal('hide');
            };
        },
        createBillingTriggerDeleteWarning: function (hasMultipleTriggers, multiMessage, singleMessage) {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="hasMultipleTriggers" type="Boolean"></param>

            var billingTriggerMessage = hasMultipleTriggers
                ? multiMessage
                : singleMessage;

            return $(document.createElement("p")).addClass('bold-red').append(billingTriggerMessage);
        },
        buildMultiSelectDeleteWarning: function (messageText, selectedRows) {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="grid" type="UL.MvcJqGrid.GridRegistration"></param>
            selectedRows = Enumerable.From(selectedRows);
            var rootElem = $(document.createElement("div"));

            var list = $(document.createElement("ul"));

            var billingTriggerCount = selectedRows.Count(function (x) { return x.ShouldTriggerBilling === "true"; }) > 1;

            var billingTriggerLabel = $(document.createElement("span"))
                .addClass('billing-trigger-label').append('(' + this.billingTriggerAnnotation + ')');

            selectedRows.ForEach(function (row) {

                var li = $(document.createElement("li")).html(row.TaskName);
                list.append(li);
                if (row.ShouldTriggerBilling) {
                    li.append(billingTriggerLabel.clone(false));
                }

            });

            rootElem.append($(document.createElement("p")).html(messageText));

            if (billingTriggerCount > 0) {
                var billingTriggerWarning = $(document.createElement("p")).addClass('bold-red')
                    .append(this.createBillingTriggerDeleteWarning(billingTriggerCount > 1));

                rootElem.append(billingTriggerWarning);
            }

            rootElem.append(list);

            return rootElem;
        },
        buildStatusChangeCompleteWarning: function (grid) {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="grid" type="UL.MvcJqGrid.GridRegistration"></param>

            var rootElem = $(document.createElement("div"));
            var primaryWarningMessage = "Are you sure you want to update multiple tasks at once? ";

            var list = $(document.createElement("ul"));

            var selectedRows = grid.getSelectedGridRows();
            var rowData = Enumerable.From(selectedRows).Select(function (x) { return x.rowData(); });
            var billingTriggerCount = rowData.Count(function (x) { return x.ShouldTriggerBilling; }) > 1;

            var billingTriggerLabel = $(document.createElement("span"))
                .addClass('billing-trigger-label').append('(' + this.billingTriggerAnnotation + ')');

            rowData.ForEach(function (row) {

                var li = $(document.createElement("li")).html(row.TaskName);
                list.append(li);
                if (row.ShouldTriggerBilling) {
                    li.append(billingTriggerLabel.clone(false));
                }

            });

            rootElem.append($(document.createElement("p")).html(primaryWarningMessage));

            if (billingTriggerCount > 0) {
                var billingTriggerWarning = $(document.createElement("p")).addClass('bold-red')
                    .append(this.createBillingTriggerDeleteWarning(true));

                rootElem.append(billingTriggerWarning);
            }

            rootElem.append(list);

            return rootElem;
        },
        getTaskStatusSelectHandler: function () {
            var self = this;

            return function (e, ui) {
                var selectors = self.getRowSelectors(ui.row);
                var taskOwnerAssigned = self.getTaskOwnerAssigned(selectors.TaskOwner);

                return self.taskStatusSelectOptions(
                   {
                       startDateElem: $(selectors.StartDate),
                       taskSelectListElem: $(selectors.TaskStatus),
                       taskOwnerAssigned: taskOwnerAssigned
                   });

            };
        },
        getStartDateChangeHandler: function () {
            var self = this;

            return function (e, ui) {
                var selectors = self.getRowSelectors(ui.row);
                var taskOwnerAssigned = self.getTaskOwnerAssigned(selectors.TaskOwner);

                return self.taskStatusSelectOptions(
                    {
                        startDateElem: $(selectors.StartDate),
                        taskSelectListElem: $(selectors.TaskStatus),
                        taskOwnerAssigned: taskOwnerAssigned
                    });
            };

        },
        getTaskOwnerChangedHandler: function () {
            var self = this;

            return function (e, ui) {
                var selectors = self.getRowSelectors(ui.row);
                var taskOwnerAssigned = self.getTaskOwnerAssigned(selectors.TaskOwner);

                return self.taskStatusSelectOptions(
                   {
                       startDateElem: $(selectors.StartDate),
                       taskSelectListElem: $(selectors.TaskStatus),
                       taskOwnerAssigned: taskOwnerAssigned
                   });
            };

        },
        getApplyColumnReadonlyRules: function () {
            var self = this;

            return function (gridRow, columnDictionary) {
                return self.readonlyFields(
                         {
                             gridRow: gridRow,
                             readonlyColumnElems: gridRow.rowData().ReadonlyFields
                         });
            };

        },
        getApplySoftwarningRules: function () {
            var self = this;

            return function (gridRows, columnDictionary) {
                var rowDataList = [];
                Enumerable.From(gridRows).ForEach(function (row) {
                    var rowData = row.rowData();
                    rowDataList.push(rowData);
                });

                return self.applySoftwarnings(rowDataList);
            };


        },
        getApplyBeforeRowEditRules: function () {
            var self = this;

            return function (gridRow, columnDictionary) {
                var selectors = self.getRowSelectors(gridRow);

                return self.beforeRowEdit({
                    allowEdits: self.getElemValueOrText(selectors.AllowEdits)
                });
            };

        },
        beforeRowEdit: function (args) {
            var defaults = {
                allowEdits: true
            };
            args = $.extend(defaults, args);
            var deferred = $.Deferred();

            if (args.allowEdits) {
                deferred.resolve(true);
            } else {
                deferred.resolve(false);
            }

            return deferred.promise();
        },
        applySoftwarnings: function (rows) {
            var self = this;
            var deferred = $.Deferred();


            self.processBillingWarning(rows).done(function (billingTriggerWarning) {
                if (billingTriggerWarning) {
                    self.processTaskOwnerWarning(rows).done(function (taskOwnerWarningResult) {
                        deferred.resolve(taskOwnerWarningResult);
                    });
                } else {
                    deferred.resolve(false);
                }
            });

            return deferred.promise();
        },
        getTaskOwnerWarningRequired: function (rowData) {
            var result = false;
            var isNotAssignedToMe = rowData.TaskOwnerAssigned !== "AssignToMe";
            var isNotProjectHandler = rowData.CurrentUser.toLowerCase() !== rowData.ProjectHandler.toLowerCase();
            var isClosingTask = rowData.TaskStatus.SelectedValue === this.taskStatusList.Completed.value
                                || rowData.TaskStatus.SelectedValue === this.taskStatusList.Canceled.value;

            if (isNotAssignedToMe && isNotProjectHandler && isClosingTask) {

                result = true;
            }

            return result;

        },
        processTaskOwnerWarning: function (argList) {
            var self = this;
            var deferred = $.Deferred();
            var primaryWarningMessage = self.taskStatusWarning;
            var rootElem = $(document.createElement("div"));
            rootElem.append($(document.createElement("p")).html(primaryWarningMessage));
            var list = $(document.createElement("ul"));
            rootElem.append(list);
            var violationCount = 0;
            Enumerable.From(argList).ForEach(function (args) {
                if (args.TaskOwnerAssigned !== "AssignToMe"
                   && args.CurrentUser.toLowerCase() !== args.ProjectHandler.toLowerCase()
                   && (args.TaskStatus.SelectedValue === self.taskStatusList.Completed.value || args.TaskStatus.SelectedValue === self.taskStatusList.Canceled.value)) {

                    violationCount++;

                    var li = $(document.createElement("li"))
                        .append($(document.createElement("span"))
                            .text("Task Id: " + args.TaskNumber))
                        .append($(document.createElement("span"))
                            .text(" - " + args.TaskName));

                    list.append(li);

                }
            });
            if (violationCount > 0) {
                self.showWarning(rootElem.htmlAll(), function (data) {
                    if (data.result === "Yes") {
                        data.modal.modal("hide");
                        deferred.resolve(true);
                    }
                    else {
                        deferred.resolve(false);
                    }

                });
            }
            else {
                deferred.resolve(true);
            }


            return deferred.promise();
        },
        processBillingWarning: function (argList) {
            var self = this;
            var deferred = $.Deferred();

            var violationCount = Enumerable.From(argList).Where(function (args) {
                return (args.ShouldTriggerBilling === "true"
                    && (args.TaskStatus.SelectedValue === "Completed" || args.TaskStatus.SelectedValue === "Canceled"));
            }).Count();

            var message = "Are you sure you want to update multiple tasks at once? ";
            var rootElem = self.buildMultiSelectDeleteWarning(message, argList);

            if (violationCount > 0) {
                self.showWarning(rootElem.htmlAll(), function (data) {
                    if (data.result === "Yes") {
                        data.modal.modal("hide");
                        deferred.resolve(true);
                    }
                    else {
                        deferred.resolve(false);
                    }
                });
            }
            else {
                deferred.resolve(true);
            }

            return deferred.promise();
        },
        taskStatusSelectOptions: function (args) {
            var self = this;
            var defaults = {
                startDateElem: null,
                taskSelectListElem: null,
                taskOwnerAssigned: false
            };
            args = $.extend(defaults, args);

            if (args.startDateElem === null || args.taskSelectListElem === null) {
                throw "startDateElem and taskSelectListElem are required.";
            }

            var selectOption = null;
            var selectedTaskStatus = args.taskSelectListElem.val();

            // If status changes to OnHold, update select list to contain RemoveHold
            if (selectedTaskStatus === self.taskStatusList.OnHold.value) {
                // If remove hold doesn't exist then add to option list
                if (args.taskSelectListElem.find("option:contains(" + self.taskStatusList.RemoveHold.display + ")").length === 0) {
                    selectOption = "<option value='" + self.taskStatusList.RemoveHold.value + "'>" + self.taskStatusList.RemoveHold.display + "</option>";
                    args.taskSelectListElem.append(selectOption);
                }
                return true;
            }

            if (selectedTaskStatus === self.taskStatusList.RemoveHold.value) {
                args.taskSelectListElem.find("option:selected").remove();
                // removed hold.  swap out options to include on hold
                if (args.taskSelectListElem.find("option:contains(" + self.taskStatusList.OnHold.display + ")").length === 0) {
                    selectOption = "<option value='" + self.taskStatusList.OnHold.value + "'>" + self.taskStatusList.OnHold.display + "</option>";
                    args.taskSelectListElem.append(selectOption);
                }
                args.taskSelectListElem.find("option:contains('" + self.taskStatusList.RemoveHold.display + "')").remove();
            }

            // validate assigned and start dates
            var startDate = args.startDateElem.val();
            if (startDate !== "" && !args.taskOwnerAssigned) {
                // Awaiting Assignment 
                args.taskSelectListElem.find("option:first").val(self.taskStatusList.AwaitingAssignment.value);
                args.taskSelectListElem.find("option:first").text(self.taskStatusList.AwaitingAssignment.display);
            }
            if ((startDate === "" && args.taskOwnerAssigned) || (startDate === "" && !args.taskOwnerAssigned)) {
                // not scheduled
                args.taskSelectListElem.find("option:first").val(self.taskStatusList.NotScheduled.value);
                args.taskSelectListElem.find("option:first").text(self.taskStatusList.NotScheduled.display);
            }
            if (startDate !== "" && args.taskOwnerAssigned) {
                // in progress                       
                args.taskSelectListElem.find("option:first").val(self.taskStatusList.InProgress.value);
                args.taskSelectListElem.find("option:first").text(self.taskStatusList.InProgress.display);
            }

            return true;
        },
        readonlyFields: function (args) {
            var self = this;
            var defaults = {
                gridRow: null,
                readonlyColumnElems: []
            };
            args = $.extend(defaults, args);

            var selectors = self.getRowSelectors(args.gridRow);
            Enumerable.From(args.readonlyColumnElems).ForEach(function (column) {
                var ctrl = $(selectors[column.FieldName]);

                if (ctrl && ctrl.length > 0) {
                    ctrl.attr("readonly", "readonly");
                    ctrl.attr("title", column.TitleDesc);

                }
            });


            return true;
        },
        getRowSelectors: function (row) {
            var self = this;
            var rowId = row.Id();
            return {
                TaskOwner: "#" + rowId + "_" + self.taskOwnerColumn,
                ReminderDate: "#" + rowId + "_" + self.reminderDateColumn,
                DueDate: "#" + rowId + "_" + self.dueDateColumn,
                ShortDescription: "#" + rowId + "_" + self.shortDiscriptionColumn,
                StartDate: "#" + rowId + "_" + self.startDateColumn,
                TaskName: "#" + rowId + "_" + self.taskNameColumn,
                TaskStatus: "#" + rowId + "_" + self.taskStatusColumn,
                IsPredefinedTask: $("#" + rowId).find(".isPredefinedTask"),
                ProjectHandler: $("#" + rowId).find(".projectHandler"),
                CurrentUser: $("#" + rowId).find(".currentUser"),
                TaskOwnerAssigned: $("#" + rowId).find(".taskOwnerAssigned"),
                TaskNumber: $("#" + rowId).find(".taskNumber"),
                AllowEdits: $("#" + rowId).find(".allowEdits"),
                ShouldTriggerBilling: $("#" + rowId).find(".shouldTriggerBilling"),
                BillingTriggerCancelMessage: $("#" + rowId).find(".billingTriggerCancelMessage"),
                BillingTriggerCompleteMessage: $("#" + rowId).find(".billingTriggerCompleteMessage"),
                ReadonlyFields: $("#" + rowId).find(".readonlyFields")
            };
        },
        getTaskOwnerAssigned: function (selector) {
            var taskOwner = $(selector).val();
            return (taskOwner !== 'Unassigned' && taskOwner !== "");
        },
        getIsPredefinedTask: function (selector) {
            return $(selector).text() || false;
        },
        getElemValueOrText: function (selector) {
            return $(selector).val() || $(selector).text();
        },
        showWarning: function (bodyMessage, callback) {
            return UL.ShowConfirm("Warning", bodyMessage, callback);
        },
        showULModalDailog: function (args, callback) {
            var options = new UL.ModalOptions();
            options.callback = callback;
            $.extend(options, args);
            UL.ShowModal(options);
        }
    };

    UL.MvcJqGrid.RegisterRenderBehavior(UL.TaskGrid.BillingTriggerAnnotation,
        function (cellvalue, column, rowObject) {
            //Adds the Billing Trigger annotation to the TaskName Column

            var addAnnotation = (rowObject.ShouldTriggerBilling !== undefined
                && (rowObject.ShouldTriggerBilling === true || rowObject.ShouldTriggerBilling === "true"));

            var wrapper = $(document.createElement("span"))
                .append($(document.createElement("span"))
                    .addClass("field-value")
                    .text(cellvalue)
            );

            if (addAnnotation) {
                wrapper.append(
                    $(document.createElement("span"))
                        .addClass("anno")
                        .addClass("billing-trigger-label")
                        .text(" (Billing annotation)")
                );
            }

            return wrapper.htmlAll();

        },
        function (cellText, column, cellvalue) {
            return $(cellvalue).find(".field-value").text();
        });


}());

$(function () {
    'use strict';
    var taskgrid = new UL.TaskGrid(UL.MvcJqGrid);
    taskgrid.init();
});
