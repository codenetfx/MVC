/// <reference path="../_references.js" />
/// <reference path="../_ULReferences.js" />
/// <reference path="../../Lib/jquery/jquery-ui-1.8.11.js" />
/// <reference path="../../Lib/jquery/plugins/jQuery.scrollTo.js" />

/* Asp.Net MVC Helper Usage
 *
 * @Html.AutoComplete("myinput", string.Empty, "/profile/SearchULUsers")
 * @Html.AutoCompleteFor(m=> m.MyProperty, "/profile/SearchULUsers") 
 * 
 */

(function ($) {
    'use strict';

    if (!window.UL) {
        window.UL = {};
    }

    var buttonClasses = {
        Add: "add",
        Remove: "remove",
        RemoveAll: "remove-all",
        AddAll: "add-all",
        MoveUp: "move-up",
        MoveDown: "move-down"
    };

    UL.DualListBoxOptions = function () {
        /// <summary>
        /// Provides a class to contain options for the UL.AutoComplete control
        /// c# Usage: @Html.AutoComplete("myinput", string.Empty, "/profile/SearchULUsers")
        /// or: @Html.AutoCompleteFor(m=> m.MyProperty, "/profile/SearchULUsers")
        /// </summary>
        ///<field name="idMember" type="String">The name of the field containing the id information within the autocomplete data list.</field>
        ///<field name="displayMember" type="String">The name of the field containing the display information within the autocomplete data list.</field>

        this.idMember = "Id";
        this.displayMember = "Display";
        this.enableShiftAll = false;
        this.enableMove = false;
        return this;
    };

    UL.DualListBox = function (target, options, ulProxy) {
        /// <summary>
        /// A classifier for extending a textbox with autocomplete functionallity.
        /// </summary>
        /// <param name="target" type="jQuery">The Target of the feature. (should be a textbox)</param>
        /// <param name="options" type="UL.AutoCompleteOptions">AutoComplete options.</param>
        /// <param name="ulProxy" type="UL.Proxy">the ajax proxy class to use for request to the server.</param>
        ///<field name="elem" type="jQuery">the html element wrapped in a jQuery that is the target of the autocomplete widget.</field>
        /// <field name="options" type="UL.DualListBoxOptions">AutoComplete options.</field>

        this.elem = target;
        this.proxy = ulProxy;
        this.options = new UL.DualListBoxOptions();
        $.extend(this.options, options);
        this.selectedItems = null;
        this.sourceSelect = null;
        this.sourceLabel = null;
        this.destSelect = null;
        this.destLabel = null;
        this.buttonEnum = null;
        this.addButton = null;
        this.removeButton = null;
        this.addAllButton = null;
        this.removeAllButton = null;
        this.moveUpButton = null;
        this.moveDownButton = null;
        this.sourceData = null;
        this.destData = null;
        return this;
    };

    UL.DualListBox.prototype = {
        init: function () {
            var self = this;

            self.sourceSelect = self.elem.find(".source select");
            self.sourceLabel = self.elem.find(".source .ul-label");
            self.destSelect = self.elem.find(".destination select");
            self.destLabel = self.elem.find(".destination .ul-label");
            self.sourceData = self.elem.find(".source datalist").data("json");
            self.destData = self.elem.find(".destination datalist").data("json");
            self.buttonEnum = Enumerable.From(self.elem.find(".buttons button"));
            self.addButton = $(self.buttonEnum.FirstOrDefault(null, function (x) { return $(x).hasClass(buttonClasses.Add); }));
            self.removeButton = $(self.buttonEnum.FirstOrDefault(null, function (x) { return $(x).hasClass(buttonClasses.Remove); }));
            self.addAllButton = $(self.buttonEnum.FirstOrDefault(null, function (x) { return $(x).hasClass(buttonClasses.AddAll); }));
            self.removeAllButton = $(self.buttonEnum.FirstOrDefault(null, function (x) { return $(x).hasClass(buttonClasses.RemoveAll); }));
            self.moveUpButton = $(self.buttonEnum.FirstOrDefault(null, function (x) { return $(x).hasClass(buttonClasses.MoveUp); }));
            self.moveDownButton = $(self.buttonEnum.FirstOrDefault(null, function (x) { return $(x).hasClass(buttonClasses.MoveDown); }));
            self.addButton.on("click", self.getAddButtonHandler());
            self.removeButton.on("click", self.getRemoveButtonHandler());
            self.addAllButton.on("click", self.getAddAllButtonHandler());
            self.removeAllButton.on("click", self.getRemoveAllButtonHandler());
            self.moveUpButton.on("mousedown", self.getMoveUpButtonHandler());
            self.moveDownButton.on("mousedown", self.getMoveDownButtonHandler());
            self.moveUpButton.on("click", self.getMoveUpClickHandler());
            self.moveDownButton.on("click", self.getMoveDownClickHandler());
            self.initControlOptions(self.options);
            self.purgeDuplicates(self.sourceData, self.destData, self.options);
            self.loadSelectbox(self.sourceSelect, self.sourceData, self.options, true);
            self.loadSelectbox(self.destSelect, self.destData, self.options);
            self.initFormIntegration();
        },
        purgeDuplicates: function(removeFromList, compareList, options) {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="removeFromList" type="Array"></param>
            /// <param name="compareList" type="Array"></param>
            /// <param name="options" type="UL.DualListBoxOptions"></param>
            Enumerable.From(Enumerable.From(removeFromList).Intersect(compareList, function(x) {
                return x[options.idMember];
            }).ToArray())
            .ForEach(function (x) {
                removeFromList.remove(x);
            });
        },
        initFormIntegration: function () {
            var self = this;
            var form = self.elem.parents("form");
            if (form.length > 0) {
                form.submit(function () {
                    self.destSelect.find("option").prop('selected', true);
                });
            }
        },
        initControlOptions: function (options) {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="options" type="UL.DualListBoxOptions"></param>

            if (!options.enableMove) {
                this.moveUpButton.hide();
                this.moveDownButton.hide();
            }

            if (!options.enableShiftAll) {
                this.removeAllButton.hide();
                this.addAllButton.hide();
            }

        },
        loadSelectbox: function (selectCtrl, data, options, applySort) {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="selectCtrl" type="jQuery"></param>
            /// <param name="data" type="Array"></param>
            /// <param name="options" type="UL.DualListBoxOptions"></param>
            if (data !== null && data.length > 0) {

                var list = Enumerable.From(data);
                if (applySort) {
                    list = list.OrderBy(function(x) { return x[options.displayMember]; });
                }
                list.ForEach(function (x) {
                    var optionTag = $(document.createElement("option"));
                    optionTag.text(x[options.displayMember]);
                    optionTag.val(x[options.idMember]);
                    optionTag.data("item", x);
                    selectCtrl.append(optionTag);
                });


            }

        },
        shiftItems: function (selectCrtl, items, applySort) {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="items" type="jQuery"></param>
            /// <param name="selectCrtl" type="jQuery"></param>
            /// <param name="applySort" type="Boolean"></param>


            var targetItemsTemp = selectCrtl.find("option");
            items = $.merge(targetItemsTemp, items);

            var sortedItems = (applySort === true)
                ? Enumerable.From(items).OrderBy(function (x) { return $(x).text(); }).ToArray()
                : items;
            items.remove();
            items.prop("selected", false);
            selectCrtl.append(sortedItems);

        },

        moveItemUp: function (items) {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="items" type="jQuery"></param>        

            var prevItem = $(items[0]).prev();
            if (prevItem.length > 0) {
                items.insertBefore(prevItem);
            }

        },
        moveItemsDown: function (items) {
            /// <summary>
            /// 
            /// </summary>
            /// <param name="items" type="jQuery"></param>

            var afterItem = items.last().next();
            if (afterItem.length > 0) {
                items.insertAfter(afterItem);
            }

        },
        getSelectedOptions: function (selectCtrl) {
            /// <summary>
            /// Hello
            /// </summary>
            /// <param name="selectCtrl" type="jQuery"></param>
            /// <returns type="jQuery"></returns>

            return selectCtrl.find('option:selected');
        },
        getAddButtonHandler: function () {
            var self = this;
            return function (e, ui) {
                e.preventDefault();
                var selectedOptions = self.getSelectedOptions(self.sourceSelect);
                self.shiftItems(self.destSelect, selectedOptions, !self.options.enableMove);
            };
        },
        getRemoveButtonHandler: function () {
            var self = this;
            return function (e, ui) {
                e.preventDefault();
                var selectedOptions = self.getSelectedOptions(self.destSelect);
                self.shiftItems(self.sourceSelect, selectedOptions, true);
            };
        },
        getAddAllButtonHandler: function () {
            var self = this;
            return function (e, ui) {
                e.preventDefault();
                if (self.options.enableShiftAll) {

                    var selectedOptions = self.sourceSelect.find("option");
                    self.shiftItems(self.destSelect, selectedOptions, !self.options.enableMove);
                }
            };
        },
        getRemoveAllButtonHandler: function () {
            var self = this;
            return function (e, ui) {
                e.preventDefault();
                if (self.options.enableShiftAll) {

                    var selectedOptions = self.destSelect.find("option");
                    self.shiftItems(self.sourceSelect, selectedOptions, true);
                }
            };
        },
        getMoveUpClickHandler: function () {
            var self = this;

            return function (e, ui) {

                e.preventDefault();

                if (self.options.enableMove) {
                    var selectedOptions = self.getSelectedOptions(self.destSelect);
                    self.moveItemUp(selectedOptions);
                }
            };
        },
        getMoveDownClickHandler: function () {
            var self = this;

            return function (e, ui) {

                e.preventDefault();

                if (self.options.enableMove) {
                    var selectedOptions = self.getSelectedOptions(self.destSelect);
                    self.moveItemsDown(selectedOptions);
                }
            };
        },
        getMoveUpButtonHandler: function () {
            var self = this;

            return function (e, ui) {

                e.preventDefault();

                if (self.options.enableMove) {
                    var t = null;
                    var start = 100;
                    var firstSelectedItem = $(self.destSelect.find("option:selected")[0]);
                    var action = function () {
                        var selectedOptions = self.getSelectedOptions(self.destSelect);
                        self.moveItemUp(selectedOptions);
                        self.destSelect.scrollTo(firstSelectedItem, { offset: -80 });
                        if (start > 20) {
                            start -= 10;
                        }
                        t = setTimeout(action, start);
                    };

                    $(this).on("mouseup", function () {
                        clearTimeout(t);
                    });

                    t = setTimeout(action, start);
                }
            };
        },
        getMoveDownButtonHandler: function () {
            var self = this;

            return function (e, ui) {
                e.preventDefault();
                if (self.options.enableMove) {
                    var t = null;
                    var start = 100;
                    var firstSelectedItem = $(self.destSelect.find("option:selected")[0]);
                    var action = function () {
                        var selectedOptions = self.getSelectedOptions(self.destSelect);
                        self.moveItemsDown(selectedOptions);
                        self.destSelect.scrollTo(firstSelectedItem, { offset: -80 });

                        if (start > 20) {
                            start -= 10;
                        }
                        t = setTimeout(action, start);
                    };

                    $(this).on("mouseup", function () {
                        clearTimeout(t);
                    });

                    t = setTimeout(action, start);
                }
            };
        }

    };


    $.fn.ulDualListBox = function (option, optionValue) {
        /// <summary>
        /// UL Dual List Box widget with MVC html extension wrapper.
        /// c# Usage: @Html.DualListBox("myinput", string.Empty, "/profile/SearchULUsers")
        /// or: @Html.DualListBoxFor(m=> m.MyProperty, "/profile/SearchULUsers")
        /// </summary>
        /// <param name="option"></param>
        /// <param name="optionValue"></param>
        /// <returns type=""></returns>

        var result = null;

        if (typeof option === 'string') {

            var amObj = $(this[0]).data("UL.DualListBox");
            if (amObj !== null) {
                try {
                    result = amObj[option](optionValue);
                }
                catch (e) {
                    throw option.toString() + " was an invalid behavior.";
                }
            }
        }
        else {

            $(this).each(function (index, $elem) {
                var elem = $($elem);
                var data = option || elem.data();
                var ctrl = new UL.DualListBox(elem, data);
                elem.data("UL.DualListBox", ctrl);
                ctrl.init();
            });
        }

        return result || $(this);

    };

}(jQuery));