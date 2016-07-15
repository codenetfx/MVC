/// <reference path="../_references.js" />
/// <reference path="../_ULReferences.js" />
/// <reference path="../../Lib/jquery/plugins/linq-vsdoc.js" />
/// <reference path="../../Lib/jquery/plugins/linq.js" />
/// <reference path="UL.AccordionMenu.js" />
/// <reference path="UL.MvcJqGrid.js" />
/// <reference path="../../Lib/jquery/jquery-ui.js" />

(function ($) {
    'use strict';
    if (!window.UL) {
        window.UL = {};
    }

    UL.FilterManager = function (elem, initOptions, providedProxy, criteria) {
        /// <summary>
        /// Provides a classifier for managing Filters
        /// </summary>
        /// <param name="elem"></param>
        /// <param name="initOptions"></param>
        /// <param name="providedProxy"></param>
        /// <param name="criteria"></param>
        var proxy = (providedProxy && providedProxy !== null)
            ? providedProxy : new UL.Proxy();


        this.criteriaField = null;
        this.viewTypeField = null;
        this.filterNameField = null;
        this.criteria = criteria;
        this.element = elem;
        this.noItemsMessage = this.element.find(".empty-message");
        this.filterContainer = this.element.find(".filter-block tbody");
        var opt = initOptions || {};
        var data = this.element.data();
        $.extend(opt, data);

        this.getOptions = function () {
            return opt;
        };

        this.getProxy = function () {
            return proxy;
        };

        this.getItemsSelector = function (key) {
            return "." + key + "_td ul li";
        };

        this.getFilterSelector = function (key) {
            return "." + key + "_td";
        };

        this.init();

    };

    UL.FilterManager.prototype = {
        init: function () {
            /// <summary>
            /// Private: Initialize a new FilterManager Object.
            /// </summary>


            //var proxy = this.getProxy();
            var options = this.getOptions();

            //register apply filters button click event handler.
            this.applyFilterBtn = $('.apply-filters-btn').on('click', this.getApplyFilterHandler());

            //register accordion menu events
            $(options.menuSelector).accordionMenu("leafClicked", this.getMenuNodeClickedHandler());
            $(options.menuSelector).accordionMenu("loadComplete", this.getMenuLoadCompleteHandler());

            //register remove click event for server rendered filter items.
            $(".filter-remove-btn").click(this.getRemoveFilterHandler());

            this.renderForm();

            //filter save button
            $("#saveLink").on("click", this.getSaveFilterHandler());

            //search button on main search bar.
            $(".search-btn").on("click", this.getSearchBarButtonClickHandler());

            //register sort links
            $(".filter-sort-btn").on("click", this.getSortClickedHandler());

            //register primary refiner clicks
            $(".refine-list a").on("click", this.getPrimaryFilterClickedHandler());


            this.initMvcJqGridEvents();
            this.initServerGeneratedMultiFilters();
            this.initMenuRefinerForActiveFilters();
            this.element.show();
        	this.applyFilterBtn.addClass('disabled');
        	this.applyFilterBtn.parent().parent().addClass('disabled');
            this.assureApplyFilterState();	
        },
        initMvcJqGridEvents: function () {
            /// <summary>
            /// Initilize jqGrid integration events
            /// </summary>

            var gridResults = $(".mvc-jqgrid");
            if (gridResults.length === 1) {
                gridResults.mvcJqGrid("onGridSorted", this.getMvcJqGridColSortedHandler());
            }
        },
        getMvcJqGridColSortedHandler: function () {
            /// <summary>
            /// Retruns an jq grid sorted event ahdnlder
            /// </summary>
            /// <returns type="Function(e, args)">The event handler.</returns>

            var self = this;
            return function (e, args) {
                self.criteria.SortField = args.sortInfo.field;
                self.criteria.SortOrder = args.sortInfo.order;
                self.criteria.Sorts = [{
                    FieldName: args.sortInfo.field,
                    Order: args.sortInfo.order
                }];
            };
        },
        getMenuLoadCompleteHandler: function () {
            /// <summary>
            /// Returns a lenu load completed handler
            /// </summary>
            /// <returns type="Function(e, args)">The event handler.</returns>

            var self = this;
            return function (e, ui) {
                self.initMenuRefinerForActiveFilters();
            };
        },
        initMenuRefinerForActiveFilters: function () {
            /// <summary>
            /// Initializes active filters.
            /// </summary>

            var self = this;

	        if (this.criteria && this.criteria.Filters) {
		        Enumerable.From(this.criteria.Filters).ForEach(function(filter) {

			        Enumerable.From(filter.Values).ForEach(function(refineValue) {

				        var convertedFilter = {
					        RefinementValue: refineValue,
					        Key: filter.Name
				        };
				        var menuItem = $(self.getOptions().menuSelector).accordionMenu("getMenuItem", convertedFilter);
				        if (menuItem !== null) {
					        $(self.getOptions().menuSelector).accordionMenu("selectItem", menuItem);
				        }

			        });
		        });
	        }
        },
        initServerGeneratedMultiFilters: function () {
            /// <summary>
            /// activate server generated multi filter sections
            /// </summary>

            var self = this;
            this.element.find(".multi-filter").each(function (index, elem) {
                self.activateAccordion($(elem).first("div"));
            });
        },
        getSearchBarButtonClickHandler: function () {
            /// <summary>
            /// Returns button click handler for the Aria Search bar button.
            /// </summary>
            /// <returns type="Function(e, args)">The event handler.</returns>

            var self = this;

            return function (e, ui) {
                var keywords = $("input#Query").val();
                if (keywords && keywords.trim() !== "") {

                    var queryFilter = {
                        RefinementValue: keywords,
                        RefinerCategory: "Keywords",
                        Key: "Query",
                        Text: keywords,
                        IsQuery: true
                    };

                    self.add(queryFilter);
                }
                else {
                    self.remove({
                        RefinementValue: keywords,
                        RefinerCategory: "Keywords",
                        Key: "Query",
                        Text: "",
                        IsQuery: true
                    });
                }

                self.getApplyFilterHandler().call(this, e);

            };
        },
        getPrimaryFilterClickedHandler: function () {
            /// <summary>
            /// 
            /// </summary>

            var self = this;

            return function (e, ui) {
                var data = $(this).data();

                e.preventDefault();

                if (data && data.refinerKey) {
                    var filterItem = {
                        RefinementValue: data.refinerValue || 'All',
                        RefinerCategory: data.refinerCategory,
                        Key: data.refinerKey,
                        Text: data.refinerText
                    };

                    self.routeFilterItem(filterItem);
                    self.getApplyFilterHandler().call(this, e);
                }
            };
        },
        getMenuNodeClickedHandler: function () {
            /// <summary>
            /// 
            /// </summary>

            var self = this;

            return function (e, ui) {

                self.routeFilterItem(self.mapAccordionEventArgsToFilterItem(ui));

                if (self.noItemsMessage.is(":visible")) {
                    self.noItemsMessage.hide();
                }
                self.assureApplyFilterState();
            };
        },

        routeFilterItem: function (filterItem) {
            if (filterItem.RefinementValue === 'All') {
                this.removeFilter(filterItem.Key);
                this.criteria.Filters.remove(function (x) { return x.Name === filterItem.Key; });
                return;
            }

            var filter = Enumerable.From(this.criteria.Filters)
                .FirstOrDefault(null, function (x) { return x.Name === filterItem.Key; });

            var tempFilterItem = null;
            var tempFilterControlFlaggedItem = null;

            if (filter) {
                tempFilterItem = Enumerable.From(filter.Values)
                     .FirstOrDefault(null, function (x) {
                         return x.toString() === filterItem.RefinementValue.toString();
                     });

                tempFilterControlFlaggedItem = Enumerable.From(filter.Values)
                    .FirstOrDefault(null, function (x) {
                        return (filterItem.ControlFlag && x.toString().contains(filterItem.ControlFlag));
                    });

                if (tempFilterControlFlaggedItem) {
                    var itemToRemove = UL.Utility.deepCopy(filterItem);
                    itemToRemove.RefinementValue = tempFilterControlFlaggedItem;
                    this.remove(itemToRemove);
                }

            }

            if (filterItem.IsQuery || tempFilterItem === null) {
                this.add(filterItem);

            } else {
                if (tempFilterItem) {
                    this.remove(filterItem);
                }
            }
        },

        hasFilter: function () {
	        if (this.criteria) {
		        return this.criteria.Filters.length > 0 || (this.criteria.Query && this.criteria.Query.trim() !== "");
	        } 
		        return false;
	        
        },

       
        assureApplyFilterState: function () {
            if (this.hasFilter()) {
                this.applyFilterBtn.removeClass('disabled');
                this.applyFilterBtn.parent().parent().removeClass('disabled');
            } 
        },

        getRemoveFilterHandler: function () {
            var self = this;
            return function (e, ui) {
                e.preventDefault();
                var filterItem = $(this).data();

                var convertedFilter = {
                    RefinementValue: filterItem.refinementValue,
                    RefinerCategory: filterItem.refinerCategory,
                    Key: filterItem.key,
                    Text: filterItem.text
                };

                $(self.getOptions().menuSelector).accordionMenu("deselectItem", convertedFilter);

                self.remove(convertedFilter);

                if (self.filterContainer.children("tr").length <= 0) {
                    self.noItemsMessage.show();
                }
            };
        },
        getSortClickedHandler: function () {
            var self = this;
            return function (e) {
                var btn = $(this);
                var sortInfo = btn.data();
                self.criteria.SortField = sortInfo.sortField;
                self.criteria.SortOrder = sortInfo.sortOrder;
                self.criteria.Sorts = [{
                    FieldName: sortInfo.sortField,
                    Order: sortInfo.sortOrder
                }];

                var ui = { isSortRequest: true };
                self.getApplyFilterHandler().call(self.applyFilterBtn, e, ui);
            };
        },
        getApplyFilterHandler: function () {
            var self = this;
            return function (e, ui) {
                if (!$(this).hasClass('disabled') || (ui && ui.isSortRequest === true)) {
                    e.preventDefault();
                    self.criteriaField.val(JSON.stringify(self.criteria));
                    self.viewTypeField.val(self.getViewType());
                    self.form.submit();
                }
            };
        },

        getSaveFilterHandler: function () {
            var self = this;

            return function (e, ui) {

                e.preventDefault();
                var textbox = $("input#Title");
                if (textbox.parents('form').valid()) {
                    self.criteriaField.val(JSON.stringify(self.criteria));
                    self.viewTypeField.val(self.getViewType());
                    self.filterNameField.val(textbox.val());
                    self.form.submit();
                }
            };
        },
        getViewType: function () {
            var queryHash = UL.GetQueryStringHash();
            if (queryHash && queryHash.hasOwnProperty("viewType")) {
                return queryHash.viewType;
            }
            return null;
        },
        mapAccordionEventArgsToFilterItem: function (eventArgs) {
            if (eventArgs) {

                var mi = eventArgs.menuItem;
                var pi = eventArgs.parentItem;

                return {
                    RefinementValue: mi.RefinementValue,
                    RefinerCategory: pi.Text,
                    Key: mi.Key,
                    Text: mi.Text,
                    ControlFlag: mi.ControlFlag
                };
            }

            return null;
        },

        add: function (filterItem) {
            /// <summary>
            /// Adds a filter Item and its data to the control.
            /// </summary>
            /// <param name="filterItem"></param>

            this.addFilterData(filterItem);
            this.addFilterItem(filterItem);
        },
        remove: function (filterItem) {
            /// <summary>
            /// Remove a Filter Item and its data to the control.
            /// </summary>
            /// <param name="filterItem"></param>

            this.removeFilterData(filterItem);
            this.removeFilterItem(filterItem);
        },
        removeFilterData: function (filterInfo) {
            /// <summary>
            /// Removes the filter data from the Search Criteria object that is tracking changes
            /// </summary>
            /// <param name="filterInfo"></param>

            var filterKey = filterInfo.Key;
            var filterValue = filterInfo.RefinementValue;

            if (filterKey === "Query") {
                this.criteria.Query = "";
                return;
            }

            var matchedFilter = Enumerable.From(this.criteria.Filters)
                .FirstOrDefault(null, function (x) {
                    return x.Name === filterKey;
                });

            if (matchedFilter) {
                matchedFilter.Values.remove(filterValue.toString());
                if (matchedFilter.Values.length <= 0) {
                    this.criteria.Filters.remove(matchedFilter);
                }
            }
        },
        getDefaultFilterObject: function (key) {
            return {
                Name: key,
                Values: []
            };
        },
        addFilterData: function (filterInfo) {
            /// <summary>
            /// Add the filter data to the Search Criteria object that is tracking changes
            /// </summary>
            /// <param name="filterInfo"></param>
            var filterKey = filterInfo.Key;
            var filterValue = filterInfo.RefinementValue;

            if (filterKey === "Query") {
                this.criteria.Query = filterValue;
                return;
            }

            var matchedFilter = Enumerable.From(this.criteria.Filters)
                .FirstOrDefault(null, function (x) {
                    return x.Name === filterKey;
                });

            if (!matchedFilter) {
                matchedFilter = this.getDefaultFilterObject(filterKey);
                this.criteria.Filters.push(matchedFilter);
            }

            matchedFilter.Values.push(filterValue);
        },

        addFilterItem: function (filterItem) {
            /// <summary>
            /// Properly adds the filter item to the control rendering.
            /// </summary>
            /// <param name="filterItem"></param>
            var filter = $(this.getFilterSelector(filterItem.Key));
            if (filter.length > 0 && !filterItem.IsQuery) {

                //switch from single fitler to multi filter 
                if (filter.find(".multi-filter").length > 0) {
                    this.addMultiFilterItem(filterItem);
                }
                else {

                    var existingItem = filter.find(".single-filter .filter-remove-btn.text");
                    var pos = existingItem.parents("tr").index();

                    var existingFilter = existingItem.data();
                    var itemTemp = {
                        RefinementValue: existingFilter.refinementValue,
                        RefinerCategory: existingFilter.refinerCategory,
                        Key: existingFilter.key,
                        Text: existingFilter.text
                    };

                    this.removeFilterItem(itemTemp);
                    var rowElem = this.renderMultiFilter(itemTemp);
                    this.addMultiFilterItem(filterItem);

                    if (pos === 0) {
                        this.filterContainer.prepend(rowElem.detach());
                    }
                    else {
                        rowElem.detach().insertAfter($(this.filterContainer[0].children[pos - 1]));
                    }

                }
            }
            else {

                if (filterItem.IsQuery) {
                    this.removeFilter(filterItem.Key);
                }

                this.renderSingleFilter(filterItem);
            }


            //make sure we have the latest reference, since the code above may have swapped the
            //filter type from single to multi
            filter = $(this.getFilterSelector(filterItem.Key));

            //add events to new anchors
            filter.find("a.needs-events")
               .on("click", this.getRemoveFilterHandler())
               .removeClass("needs-events");
        },

        removeFilterItem: function (filterItem) {
            /// <summary>
            /// Properly Removes a filter Item from the control rendering.
            /// </summary>
            /// <param name="filterItem"></param>

            var elemArray = this.element.find(this.getItemsSelector(filterItem.Key)).toArray();
            if (elemArray.length > 1) {
                Enumerable.From(elemArray).Where(function (x) {
                    return $(x).find("a.text").data().refinementValue === filterItem.RefinementValue;
                }).ForEach(function (x) {
                    $(x).remove();
                });
            }
            else {
                //single filter just remove whole thing
                this.removeFilter(filterItem.Key);
            }

            var remainingItems = this.element.find(this.getItemsSelector(filterItem.Key));
            if (remainingItems.length === 1) {
                var jqLinkElem = remainingItems.find("a.text");
                var tempFilterItem = jqLinkElem.data();
                var pos = jqLinkElem.parents("tr").index();

                this.removeFilter(tempFilterItem.key);
                var rowElem = this.renderSingleFilter({
                    Key: tempFilterItem.key,
                    Text: tempFilterItem.text,
                    RefinementValue: tempFilterItem.refinementValue,
                    RefinerCategory: tempFilterItem.refinerCategory
                });

                if (pos === 0) {
                    this.filterContainer.prepend(rowElem.detach());
                }
                else {
                    rowElem.detach().insertAfter($(this.filterContainer[0].children[pos - 1]));
                }

                //add events to new anchors
                this.element.find("a.needs-events")
                   .on("click", this.getRemoveFilterHandler())
                   .removeClass("needs-events");
            }

        },

        removeFilter: function (filterKey) {
            /// <summary>
            /// Private: Removes all all matching filter renderings using specified fitlerKey
            /// </summary>
            /// <param name="filterKey"></param>

            this.element.find(this.getFilterSelector(filterKey))
                .parent().remove();
        },

        addMultiFilterItem: function (filterItem) {
            /// <summary>
            /// Private: taks a filterItem and renders it as a multi Filter Item.
            /// </summary>
            /// <param name="filterItem" type="FilterItem">The data item</param>

            var li = this.renderMultiFilterItem(filterItem);
            var containerSelector = "." + filterItem.Key + "_td ul";
            var ul = this.element.find(containerSelector);

            if (ul.length > 0) {
                ul.append(li);
            }
        },

        renderSingleFilter: function (filterItem) {
            /// <summary>
            /// Renders the single filter structure including the filter item.
            /// </summary>
            /// <param name="filterItem">The data item</param>

            var filterClassKey = this.getFilterSelector(filterItem.Key).replace(".", "");
            var row = UL.Utility.createTag("tr");
            row.append(UL.Utility.createTag("td")
                .addClass("last-child")
                .addClass(filterClassKey)
                .append(UL.Utility.createTag("div")
                    .addClass("single-filter")
                    .append(UL.Utility.createTag("a")
                        .addClass("filter-remove-btn text needs-events")
                        .data("key", filterItem.Key)
                        .data("refinementValue", filterItem.RefinementValue)
                        .data("refinerCategory", filterItem.RefinerCategory)
                        .data("text", filterItem.Text)
                        .text(filterItem.RefinerCategory + ":" + filterItem.Text)
                    )
                    .append(UL.Utility.createTag("a")
                        .addClass("filter-remove-btn icon-rem pull-right needs-events")
                        .data("key", filterItem.Key)
                        .data("refinementValue", filterItem.RefinementValue)
                        .data("refinerCategory", filterItem.RefinerCategory)
                        .data("text", filterItem.Text)
                    )
                )
                );

            this.filterContainer.append(row);
            return row;
        },

        renderMultiFilterItem: function (filterItem) {
            /// <summary>
            /// Renders an individual item for the multi filter structure
            /// </summary>
            /// <param name="filterItem"></param>
            /// <returns type="">returns the item rendering in a li tag jqResults object</returns>

            var li = $(UL.Utility.createTag("li"));

            li.append(UL.Utility.createTag("a")
                .addClass("filter-remove-btn text needs-events")
                .data("key", filterItem.Key)
                .data("refinementValue", filterItem.RefinementValue)
                .data("refinerCategory", filterItem.RefinerCategory)
                .data("text", filterItem.Text)
                .text(filterItem.Text)
            )
            .append(UL.Utility.createTag("a")
                .addClass("filter-remove-btn icon-rem pull-right needs-events")
                .data("key", filterItem.Key)
                .data("refinementValue", filterItem.RefinementValue)
                .data("refinerCategory", filterItem.RefinerCategory)
                .data("text", filterItem.Text)
                );

            return li;
        },

        renderMultiFilter: function (filterItem) {
            /// <summary>
            /// Renders the primary Multi Filter structure and the first filter Item.
            /// </summary>
            /// <param name="filterItem"></param>

            var filterClassKey = this.getFilterSelector(filterItem.Key).replace(".", "");
            var row = UL.Utility.createTag("tr");

            row.append(UL.Utility.createTag("td")
                .addClass("last-child")
                .addClass(filterClassKey)
                .append(UL.Utility.createTag("div")
                    .addClass("multi-filter")
                    .append(UL.Utility.createTag("h3")
                        .text(filterItem.RefinerCategory)
                    )

                    .append(UL.Utility.createTag("div")
                        .append(UL.Utility.createTag("ul")
                            .append(this.renderMultiFilterItem(filterItem))
                        )
                    )
                )
                );

            this.filterContainer.append(row);
            this.activateAccordion(row.find(".multi-filter").first("div"));
            return row;
        },
        activateAccordion: function (jqElem) {
            /// <summary>
            /// Applies the jquery ui accordian widget to the specified jqElement.
            /// </summary>
            /// <param name="jqElem" type="jqResult">The jquery result object 
            /// containing the html element to which the jquery ui accordning 
            /// widget will be applied. </param>

            jqElem.accordion({
                header: "h3",
                collapsible: true,
                heightStyle: "content",
                icons: {
                    "header": "ui-icon-plus", "activeHeader": "ui-icon-minus"
                }
            });
        },
        renderForm: function () {
            /// <summary>
            /// Private: creates a hidden form for 
            /// submitting critera, viewtype to allow filter 
            /// configuration to me named and saved for the user.
            /// </summary>

            this.criteriaField = UL.Utility.createTag("input")
                    .prop("type", "hidden")
                    .prop("name", "searchCriteria")
                    .prop("id", "filterSearchCriteria");

            this.viewTypeField = UL.Utility.createTag("input")
                    .prop("type", "hidden")
                    .prop("id", "viewType")
                    .prop("name", "viewType");

            this.filterNameField = UL.Utility.createTag("input")
                    .prop("type", "hidden")
                    .prop("id", "saveFilterName")
                    .prop("name", "saveFilterName");

            this.form = $(UL.Utility.createTag("form"))
                .prop("method", "POST")
                .prop("action", this.getOptions().url)
                .append(this.criteriaField)
                .append(this.viewTypeField)
                .append(this.filterNameField);
            this.element.append(this.form);
        }
    };

    $.fn.filterManager = function (options, optionValue) {
        /// <summary>
        /// Initializes a new Filter Manager widget, or makes api calls.
        /// Note when options parameter is a string it makes api calls, otherwise if null or type object trys to initialize.
        /// </summary>
        /// <param name="options" type="Object/String">The initialization options or if string the name of the api method to call.</param>
        /// <param name="optionValue" type="Object">The value passed when using widget interface to make api calls.</param>
        /// <returns type="jqResult">Returns the jquery Result object.</returns>

        if (typeof options === 'string') {

            var amObj = $(this[0]).data("UL.FilterManager");
            if (amObj !== null) {
                try {
                    amObj[options](optionValue);
                }
                catch (e) {
                    throw options.toString() + " was an invalid behavior.";
                }
            }
        }
        else {

            $(this).each(function (index, $elem) {
                var elem = $($elem);

                //as an intermediate design the criteria will be
                //rendered into a tag, once ko is implemented this control 
                //show have direct access to the object.
                var criteriaTag = $('#criteria').data();
                var criteriaJson = null;

                if (criteriaTag) {
                    criteriaJson = criteriaTag.json;
                }

                elem.data("UL.FilterManager", new UL.FilterManager(elem, null, new UL.Proxy(), criteriaJson));
            });
        }

        return $(this);
    };

	$.fn.auxiliaryFilterBtn = function(filterManager) {
		/// <summary>
		/// Initializes Auxiliary Filter Buttons to apply to the specified filterManager.
		/// </summary>   
		/// <param name="filterManager">Reference to the filterManager button click events will apply.</param>
		var elements = $(this);
		elements.each(function(index, elem) {
			$(elem).click(function(e, ui) {
				///<var name="fm" type="UL.FilterManager">The filter manager class.</var> 
				var fm = $(filterManager).data("UL.FilterManager");
				var filterData = $(this).data();

				var filterItem = {
					RefinementValue: filterData.refinementValue,
					RefinerCategory: filterData.filterParentText,
					Key: filterData.key,
					Text: filterData.filterText
				};

				fm.routeFilterItem(filterItem);

				if (fm.noItemsMessage.is(":visible")) {
					fm.noItemsMessage.hide();
				}
				fm.assureApplyFilterState();
				fm.applyFilterBtn.trigger("click");
			});
		});
	};


}(jQuery));




