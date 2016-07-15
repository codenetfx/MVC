/// <reference path="../_references.js" />
/// <reference path="../_ULReferences.js" />


(function () {
    'use strict';
    if (!window.UL) {
        window.UL = {};
    }

    ///Document Manager
    UL.DocumentManager = function (options) {
    	/// <summary>
        /// Docment Management widget that provdes a ui for associating/disasociating 
        /// documents between projects and task entities,.
    	/// </summary>
    	/// <param name="options">Configuration options</param>
        /// <returns type="UL.DocumentManager">Instance of the document Manager.</returns>

        this.sectionSelector = null;
        this.section = null;

        this.associatedDocContainer = null;
        this.associatedDocItemTemplate = null;
        this.associatedContainerClass = null;
        this.taskDocs = null;
        this.originalDocLinks = null;
        this.taskDocCountLabel = null;

        this.projectDocContainer = null;
        this.projectDocItemTemplate = null;
        this.projectContainerClass = null;
        this.projectDocs = null;
        this.projectDocCountLabel = null; 


        this.form = null;
        this.formDataField = null;
        this.refreshButton = null;

        this.progressAnimation = null;
        this.request = null;
        this.noLinksMsgObj = null;


        return this;
    };

    UL.DocumentManager.prototype = {
        init: function (selector) {
        	/// <summary>
        	/// Private: initializes the document manager
        	/// </summary>
            /// <param name="selector" type="String">Jquery selector string for the primary 
            /// object for which the Document manager will attach itself.</param>
            /// <returns type="UL.DocumentManager">the document manager instance with init changes.</returns>

            var self = this;
            self.sectionSelector = selector;
            self.section = $(self.sectionSelector);
            var itemTemplateClass = 'item-template';

            //init request obj if json was provided.
            self.loadRequestData();

            //setup associated doc item template and container           
            self.associatedContainerClass = self.section.data("associatedContainerClass");
            self.associatedDocContainer = self.section.find("." + self.associatedContainerClass);
            self.associatedDocItemTemplate = self.getTemplate(self.associatedDocContainer, itemTemplateClass);
            self.taskDocCountLabel = self.section.find(".task-doc-count");

            //setup project doc item template and container

            self.projectContainerClass = self.section.data("projectContainerClass");
            self.projectDocContainer = self.section.find("." + self.projectContainerClass);
            self.projectDocItemTemplate = self.getTemplate(self.projectDocContainer, itemTemplateClass);
            self.projectDocCountLabel = self.section.find(".proj-doc-count");

            //init form filed
            self.registerFormDataField(".task-edit-form");

            //no links msg obj
            self.noLinksMsgObj = $('.no-docs-msg');

            //init loading animation
            self.progressAnimation = $($.fn.modal.defaults.spinner).css("position", "relative")
                .css("zindex", "1000").css("top", "50px");

            self.section.find('.refresh').hide();
            self.refreshButton = self.section.find('.refresh').click(function (e, data) {
                self.Refresh();
            });

            self.registerAddButton();
            self.registerRemoveButton();
            self.registerSelectAllCheckboxes();

            self.Refresh();
            return self;
        },

        Refresh: function () {
        	/// <summary>
        	/// Refreshes the data from the server and redraws the control.
            /// </summary>

            var self = this;
            self.showLoadAnimation();

            $.ajax({
                type: 'POST',
                contentType: 'application/json',
                url: self.section.data("url"),
                data: JSON.stringify(self.request),
                success: function (data) {
                    if (data && data.Successful) {
                        var temp = null;

                        if (data.TaskDocumentResults) {
                            temp = data.TaskDocumentResults;
                            self.taskDocs = (temp !== null && temp.length > 0)
                                ? temp
                                : [];

                            self.originalDocLinks = JSON.parse(JSON.stringify(temp));
                        }

                        if (data.ProjectDocumentResults) {
                            temp = data.ProjectDocumentResults;
                            self.projectDocs = (temp !== null && temp.length > 0)
                                ? temp
                                : [];
                        }

                        if (data.Request) {
                            self.request = data.Request;
                        }

                        self.Render();
                    }
                    else {
                        if (data) {
                            if (data && !(data.Successful === undefined)) {
                                UL.HandleAjaxError(data);
                            }
                            else {
                                UL.HandleAjaxUnhandledErrorContentAll(data);
                            }
                        }
                    }
                },
                complete: function (e) {
                    self.hideLoadAnimation();
                },
                error: function (jqXHR, textStatus, errorThrown) {
                	UL.HandleAjaxUnhandledError(jqXHR, textStatus, errorThrown);
                },
                always: function (e) {
                    self.hideLoadAnimation();
                }
            });
        },

        Render: function () {
        	/// <summary>
        	/// Draws the control using html.
            /// </summary>

            try {
                var self = this;
                self.renderItems(self.taskDocs, self.associatedDocItemTemplate, self.associatedDocContainer);
                self.renderItems(self.projectDocs, self.projectDocItemTemplate, self.projectDocContainer);
                self.renderHeaderSummary(self.taskDocs.length, self.projectDocs.length);
            }
            catch (ignore) {

            }
        },

        renderHeaderSummary: function (taskDocCount, projDocCount) {
        	/// <summary>
        	/// Draws the header summary.
        	/// </summary>
        	/// <param name="taskDocCount" type="Int">Total documents associated to he task</param>
            /// <param name="projDocCount" type="Int">Total documents associated to the project</param>

            var self = this;
            self.taskDocCountLabel.text(taskDocCount);
            self.projectDocCountLabel.text(projDocCount);

            if (taskDocCount <= 0) {
                self.noLinksMsgObj.show();
            }
            else {
                self.noLinksMsgObj.hide();
            }
        },

        renderItems: function (itemList, itemTemplate, container) {
        	/// <summary>
        	/// Private: Draws the list of document items using the specified item template to the specified container.
        	/// </summary>
        	/// <param name="itemList" type="Array">List of items to be drawn.</param>
        	/// <param name="itemTemplate" type="string">Html item temlate.</param>
            /// <param name="container" type="jqResult">The container reference.</param>

            var self = this,
                i = 0,
                htmlItem = null;

            for (i = 0; i < itemList.length; i++) {
                htmlItem = self.mapItemToTemplate(itemTemplate, itemList[i]);
                container.append(htmlItem);
            }
        },

        registerFormDataField: function (formSelector) {
        	/// <summary>
            /// Private: integrates the task document information 
            /// into an existing post form, for saving.
        	/// </summary>
            /// <param name="formSelector" type="String">jquery selector string to find the 
            /// form for which the taskDocument field will be attached.</param>

            var self = this;
            self.form = $(formSelector);
            self.formDataField = $(document.createElement("input"))
                .attr("type", "hidden")
                .attr("name", "taskDocuments")
                .attr("id", "taskDocuments");
            self.form.append(self.formDataField);
        },

        loadRequestData: function () {
        	/// <summary>
        	/// Private: Retrieves the mvc html printed json request object from a datalist tag.
            /// </summary>

            var self = this;
            var dataHolder = self.section.find("dataList.request");
            if (dataHolder.length > 0) {
                self.request = dataHolder.data("raw");
                dataHolder.remove();
            }
        },

        getTemplate: function (container, templateClass) {
        	/// <summary>
        	/// Retreives the item template, and cleans the control display.
        	/// </summary>
        	/// <param name="container" type="jqResult">The container that holds the template.</param>
        	/// <param name="templateClass" type="string">the css class used to find the template</param>
            /// <returns type="String">The tempate in an html string.</returns>

            //retrieve template, clean, and remove from DOM.
            var templateObj = container.find('.' + templateClass);
            templateObj.removeClass(templateClass);
            templateObj.remove();
            return templateObj.htmlAll();
        },

        serializeChanges: function () {
        	/// <summary>
            /// Private: serialize changes to document associations, and stores them into the 
            /// form field for later posting to the server.
            /// </summary>

            var self = this;
            var docChangeResults = {
                Original: self.originalDocLinks,
                Current: self.taskDocs
            };

            var data = JSON.stringify(docChangeResults);
            self.formDataField.val(data);
        },
        clearSelectAllCheckBoxes: function () {
        	/// <summary>
        	/// Clears the Select All check box
            /// </summary>

            var self = this;
            self.section.find(".select-all").prop("checked", false);
        },
        registerSelectAllCheckboxes: function () {
        	/// <summary>
        	/// Registers the change event to the select All checkbox.
            /// </summary>

            var self = this;
            self.section.find(".select-all").change(function (e) {
                var chk = $(this);
                var parentTable = chk.parents("table");
                parentTable.find("tbody").find(".select-item")
                    .prop("checked", chk.prop("checked"));
            });

        },

        registerAddButton: function () {
        	/// <summary>
        	/// Private: Registers the click event handler to the add document button.
            /// </summary>

            var self = this;

            self.section.find(".add-docs").click(function (e) {
                var selectedCbx = self.projectDocContainer.find(".select-item:checked");
                selectedCbx.prop("checked", false);
                var selectedRows = selectedCbx.parents("tr");
                selectedRows.remove();
                self.associatedDocContainer.append(selectedRows);
                var i = 0,
                    row = null,
                    itemId = '',
                    dataItem = null;

                var findDataItem = function (collection, searchId) {
                    return Enumerable.From(self.projectDocs)
                        .First(function (x) { return x.Id === searchId; });
                };

                for (i = 0; i < selectedRows.length; i++) {
                    row = $(selectedRows[i]);
                    itemId = row.data("id");
                    dataItem = findDataItem(self.projectDocs, itemId);
                    if (dataItem !== null) {
                        self.taskDocs.push(dataItem);
                        self.projectDocs.remove(dataItem);
                    }
                }

                self.renderHeaderSummary(self.taskDocs.length, self.projectDocs.length);
                self.serializeChanges();
                self.clearSelectAllCheckBoxes();
            });
        },

        registerRemoveButton: function () {
        	/// <summary>
        	/// Private: Registers the click event handler to the remove document button.
            /// </summary>

            var self = this;

            self.section.find(".remove-docs").click(function (e) {
                var selectedCbx = self.associatedDocContainer.find(".select-item:checked");
                selectedCbx.prop("checked", false);
                var selectedRows = selectedCbx.parents("tr");
                selectedRows.remove();
                self.projectDocContainer.append(selectedRows);
                var i = 0,
                    row = null,
                    itemId = '',
                    dataItem = null;

                var findDataItem = function (collection, searchId) {
                    return Enumerable.From(self.taskDocs)
                       .First(function (x) { return x.Id === searchId; });
                };

                for (i = 0; i < selectedRows.length; i++) {
                    row = $(selectedRows[i]);
                    itemId = row.data("id");
                    dataItem = findDataItem(self.taskDocs, itemId);

                    if (dataItem !== null) {
                        self.projectDocs.push(dataItem);
                        self.taskDocs.remove(dataItem);
                    }
                }

                self.renderHeaderSummary(self.taskDocs.length, self.projectDocs.length);
                self.serializeChanges();
                self.clearSelectAllCheckBoxes();
            });

        },

        showLoadAnimation: function () {
        	/// <summary>
        	/// Presents loading animation to the user.
            /// </summary>

            var self = this;
            if (self.progressAnimation !== null) {
                self.section.append(self.progressAnimation);
            }
        },

        hideLoadAnimation: function () {
        	/// <summary>
        	/// Hides the loading animation from the user.
            /// </summary>

            var self = this;
            if (self.progressAnimation !== null) {
                self.progressAnimation.remove();
            }
        },

        mapItemToTemplate: function (templateHtmlStr, item) {
        	/// <summary>
        	/// Private: Merges a single data item into the Item Template html and returns resulting string.
        	/// </summary>
        	/// <param name="templateHtmlStr" type="String">the html template as a stirng.</param>
        	/// <param name="item" type="Object">the data to be used to replace placeholders in the template</param>
            /// <returns type="String">The merged html and data in a string.</returns>

            return UL.TemplateEngine(templateHtmlStr, item);
        }
    };
    

    //jquery plugin to api for Document Manager
    $.fn.documentManager = function (options) {
    	/// <summary>
        /// Initialize UL Document manager against the first html object in the jqResult collection.
        /// Provides api access to an existing Document manager instance.
    	/// </summary>
    	/// <param name="options">Configuration options.</param>
        /// <returns type="jqResult">The jquery result object.</returns>

        if (this.length > 0) {
            var htmlobj = this[0];

            if (!htmlobj.control) {
                htmlobj.control = new UL.DocumentManager(options)
                    .init(this.selector);
            }
        }

        return this;
    };



}(jQuery));


