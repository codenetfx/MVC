/// <reference path="../_references.js" />
/// <reference path="../_ULReferences.js" />
/// <reference path="../../Lib/jquery/jquery-ui-1.8.11.js" />

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

    UL.AutoCompleteOptions = function(){
    	/// <summary>
        /// Provides a class to contain options for the UL.AutoComplete control
        /// c# Usage: @Html.AutoComplete("myinput", string.Empty, "/profile/SearchULUsers")
        /// or: @Html.AutoCompleteFor(m=> m.MyProperty, "/profile/SearchULUsers")
        /// </summary>
        ///<field name="minTermLength" type="Int">The miniumum length of the search term before a request is fired to the server.</field>
        ///<field name="url" type="String">The url for which the search request is sent.</field>
        ///<field name="idMember" type="String">The name of the field containing the id information within the autocomplete data list.</field>
        ///<field name="displayMember" type="String">The name of the field containing the display information within the autocomplete data list.</field>
        ///<field name="itemRenderFunc" type="String">Custom function used to override the default item rendering function.</field>
        ///<field name="maxScrollItems" type="String">Maximum number of items to show before adding scroll to overflow.</field>

        this.minTermLength = 3;
        this.url = '/default';
        this.idMember = "Id";
        this.displayMember = "Display";
        this.itemRenderFunc = null;
        this.maxScrollItems = 5;
        return this;
    };

    UL.AutoComplete = function (target, options, ulProxy) {
        /// <summary>
        /// A classifier for extending a textbox with autocomplete functionallity.
        /// </summary>
        /// <param name="target" type="jQuery">The Target of the feature. (should be a textbox)</param>
        /// <param name="options" type="UL.AutoCompleteOptions">AutoComplete options.</param>
        /// <param name="ulProxy" type="UL.Proxy">the ajax proxy class to use for request to the server.</param>
        ///<field name="elem" type="jQuery">the html element wrapped in a jQuery that is the target of the autocomplete widget.</field>
        /// <field name="options" type="UL.AutoCompleteOptions">AutoComplete options.</field>

        this.elem = target;
        this.proxy = ulProxy;
        this.options = new UL.AutoCompleteOptions();
        $.extend(this.options, options); 
        this.proxyOptions = new UL.ProxyOptions();
        this.proxyOptions.enableBlocking = false;
        this.proxyOptions.enableKo = false;
        this.proxyOptions.enableLoadAnamation = false;
        this.selectedItem = null;
        this.hiddenField = null;
        this.lastValue = this.elem.val();
        this.init();
        return this;
    };

    UL.AutoComplete.prototype = {
        init: function () {
            var self = this;
        
            /*the jslint-exeption is for the "_" used by the jquery plugin on              
             the renderItem method that needs overridden. */

            /*jslint nomen:true*/
            var autocompleteObj = $(self.elem).autocomplete({
                minLength: self.options.minTermLength,
                source: function(request, response) {
                    self.processRequest(request, response);
                },
                select: function(event, ui) {
                    /// <summary>
                    /// jquery-ui widget select event hander
                    /// </summary>
                    /// <param name="event" type="Event"></param>
                    /// <param name="ui"></param>

                    event.preventDefault();
                    self.select(ui.item);
                    self.elem.trigger("selected", ui);
                    return false;
                },
                focus: function(event, ui) {
                    event.preventDefault();
                    self.select(ui.item);
                    return false;
                }
            });

            autocompleteObj.autocomplete("instance")._renderItem = function (ul, item) {
                return self.renderItem(ul, item);
            };
            $(self.target).overflow = "auto";
            $(self.target).maxHeight = "10px";
	        var hf = $(self.elem).next();
	        if (hf.length > 0) {
	        	this.hiddenField = hf;
                this.configureValuePost();
                this.registerBlurHandler();
				
            }

            autocompleteObj.autocomplete("instance")._resizeMenu = function() {
                var ul, lis, ulW, barW;
                if (isNaN(self.options.maxScrollItems)) {
                    return;
                }
                ul = this.menu.element
                    .css({ overflowX: '', overflowY: '', width: '', maxHeight: '' }); // Restore
                lis = ul.children('li').css('whiteSpace', 'nowrap');

                if (lis.length > self.options.maxScrollItems) {
                    ulW = ul.prop('clientWidth');
                    ul.css({
                        overflowX: 'hidden',
                        overflowY: 'auto',
                        maxHeight: lis.eq(0).outerHeight() * self.options.maxScrollItems + 1
                    }); // 1px for Firefox
                    barW = ulW - ul.prop('clientWidth');
                    ul.width('+=' + barW);
                }

                // Original code from jquery.ui.autocomplete.js _resizeMenu()
                ul.outerWidth(Math.max(
                    ul.outerWidth() + 1,
                    this.element.outerWidth()
                ));
            };

            self.elem.on("blur", self.getOnBlurHandler());

        },
        getOnBlurHandler: function () {

            var self = this;

            return function (e, ui) {
                var currentVal = $(this).val();
                if (currentVal !== self.lastValue) {
                    self.elem.trigger("change", ui);
                }
            };
        },
        registerSelectEventHandler: function (handler) {
            var self = this;
            $(self.elem).on("selected", handler);
        },
        resetValueIfEmpty: function () {
            var self = this;
            if (self.elem.val() === "") {
                self.hiddenField.val("");
                self.elem.val("");
                self.elem.text("");
                self.selectedItem = null;
            }
        },
        registerBlurHandler: function () {
            var self = this;
            $(this.elem).on("blur", function (e, ui) {
                var jqElem = $(this);
                if (self.options.postValue) {

                    if (self.selectedItem === null
                        || self.selectedItem[self.options.displayMember] !== jqElem.val()) {

                        jqElem.autocomplete({
                            disabled: true
                        });

                        jqElem.val("");
                        jqElem.text("");
                        self.hiddenField.val("");
                        self.selectedItem = null;
                        jqElem.autocomplete({
                            disabled: false
                        });

                    }
                }
                self.resetValueIfEmpty();
            });

        },
        configureValuePost: function () {
	        return;
	        ///// <summary>
	        ///// Overrides the posted value of the textbox
	        ///// by adding a hidden field with the same name
	        ///// </summary>
	        //var jqElem = $(this.elem);
	        //var elemName = jqElem.prop("name");
	        //var fieldName = jqElem.data("postValueField") || jqElem.prop("name");

	        //var elemId = jqElem.data("postValueField") || jqElem.prop("id");

	        //this.hiddenField = $(document.createElement("input"))
	        //    .prop("type", "hidden")
	        //    .prop("id", elemId + "_hidden")
	        //    .prop("name", fieldName);
	        //jqElem.after(this.hiddenField);

	        //jqElem.prop("name", elemName + "_display");
        },

        processRequest: function (request, response) {
        	/// <summary>
        	/// Process the request for search matches to the server.
        	/// </summary>
        	/// <param name="request">the request object to send the search term to the server.</param>
            /// <param name="response" type="Function(data)">the method to be called to render 
            /// the dropdown list based on the data object list specified.</param>
            var self = this;

            var data = {
                keyword: request.term
            };

            this.proxyCallback = function (successful, data, errors) {
                if (successful) {
                    self.currentResults = data.Data;
                    response(data.Data);
                }
                else {
                    window.console.log("UL.AutoComplete request to server was unsuccessful.");
                }
            };

            self.proxy.send(data, self.options.url, this.proxyCallback, self.proxyOptions);
        },
        proxyCallback:function(successful, data, errors){
        	/// <summary>
        	/// Private: 
        	/// </summary>
        	/// <param name="successful"></param>
        	/// <param name="data"></param>
            /// <param name="errors"></param>

            //this is an empty delegate, that is 
            //overridden in processRequest method.
            return;

        },
        getSelectedItem: function () {
            return this.selectedItem;
        },
        clearSelectedItem: function () {
            this.elem.val('');
            this.selectedItem = {};
        },
        select: function (item) {
        	/// <summary>
        	/// Selects the specified data item
        	/// </summary>
            /// <param name="item">The data item to be selected</param>

            var self = this;
           
            $(self.elem).val(item[self.options.displayMember])
                .data("itemId", item[self.options.idMember])
                .data("item", item);
            self.selectedItem = item;

            if (this.hiddenField) {
                this.hiddenField.val(item[self.options.idMember]);
            }
        },
        selectIndex: function (index) {
            if (this.currentResults !== undefined
                && this.currentResults !== null
                && this.currentResults.length > 0) {

                var temp = this.currentResults[index];
                this.select(temp);
                var ui = {
                    item: temp
                };
                this.elem.trigger("selected", ui);
            }
        },
        renderItem: function (ul, item) {
            /// <summary>
            /// Providers the default implemenation to render an item in the dropdown menu, 
            /// and provides a item rendering override hook via options.ItemRenderFunc
            /// </summary>
            /// <param name="ul" type="jQuery">Refernece to the unordered list.</param>
            /// <param name="item">The Data object to be bound</param>
            /// <returns type="jQuery">The list item (li) jQuery Refernece.</returns>

            var self = this;

            if (self.options.itemRenderFunc
                 && typeof self.options.itemRenderFunc === 'function') {
                return self.options.itemRenderFunc(ul, item);
            }
        

            var isEven = (((ul.children().length + 1) % 2) === 0);
            var li = $("<li>");
            var a = $("<a>");

            a.append($("<div>")
                .addClass("name-text")
                .text(item[self.options.displayMember])
            );
         
            if (item.hasOwnProperty(self.options.descriptionMember)
                && item[self.options.descriptionMember]
                && item[self.options.descriptionMember].trim() !== '') {
                a.append($("<div>")
                   .addClass("desc-text")
                   .text(item[self.options.descriptionMember])
               );
            }

            li.prop("id", item[self.options.idMember])
               .data("item", item)
               .addClass((isEven ? "even" : ""))
               .append(a);

               return li.appendTo(ul);
        }
    };


    $.fn.ulAutoComplete = function (option, optionValue) {
    	/// <summary>
        /// UL Auto complete widget with MVC html extension wrapper.
        /// c# Usage: @Html.AutoComplete("myinput", string.Empty, "/profile/SearchULUsers")
        /// or: @Html.AutoCompleteFor(m=> m.MyProperty, "/profile/SearchULUsers")
    	/// </summary>
    	/// <param name="option"></param>
    	/// <param name="optionValue"></param>
        /// <returns type=""></returns>

        var result = null;

        if (typeof option === 'string') {

            var amObj = $(this[0]).data("UL.AutoComplete");
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
                elem.data("UL.AutoComplete", new UL.AutoComplete(elem, data, new UL.Proxy()));              
            });
        }

        return result || $(this);

    };

}(jQuery));