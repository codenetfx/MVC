/// <reference path="../_references.js" />
/// <reference path="../_ULReferences.js" />
/// <reference path="../../Lib/jquery/jquery-ui-1.8.11.js" />

/* Asp.Net MVC Helper Usage
 *
 * @Html.Combobox("myinput", string.Empty, "/profile/SearchULUsers")
 * @Html.ComboboxFor(m=> m.MyProperty, "/profile/SearchULUsers") 
 * 
 */

(function ($) {
	'use strict';

	if (!window.UL) {
		window.UL = {};
	}

	UL.ComboboxOptions = function () {
		/// <summary>
		/// Provides a class to contain options for the UL.Combobox control
		/// c# Usage: @Html.Combobox("myinput", string.Empty, "/profile/SearchULUsers")
		/// or: @Html.ComboboxFor(m=> m.MyProperty, "/profile/SearchULUsers")
		/// </summary>
		///<field name="itemRenderFunc" type="String">Custom function used to override the default item rendering function.</field>
		///<field name="maxScrollItems" type="String">Maximum number of items to show before adding scroll to overflow.</field>

		this.itemRenderFunc = null;
		this.maxScrollItems = 5;
		this.displayMember = "label";
		this.idMember = "value";
		this.descriptionMember = "desc";
		return this;
	};

	UL.Combobox = function (target, options) {
		/// <summary>
		/// A classifier for extending a textbox with Combobox functionallity.
		/// </summary>
		/// <param name="target" type="jQuery">The Target of the feature. (should be a textbox)</param>
		/// <param name="options" type="UL.ComboboxOptions">Combobox options.</param>		
		/// <field name="elem" type="jQuery">the html element wrapped in a jQuery that is the target of the Combobox widget.</field>
		/// <field name="options" type="UL.ComboboxOptions">Combobox options.</field>

		this.elem = target;
		this.options = new UL.ComboboxOptions();
		$.extend(this.options, options);	
		this.selectedItem = null;
		this.textCtrl = null;
		this.hiddenTextCtrl = null;
		this.hiddenValueCtrl = null;
		this.itemList = null;
		this.btn = null;
		this.isOpen = null;
		return this;
	};

	UL.Combobox.prototype = {
		init: function () {
			var self = this;
			self.btn = self.elem.find("button");
			self.textCtrl = self.elem.find(".tbox");
			self.hiddenTextCtrl = self.elem.find(".model-text");
			self.hiddenValueCtrl = self.elem.find(".model-value");

			self.hiddenTextCtrl.attr("id", self.options.modelTextBinding);
			self.hiddenTextCtrl.attr("name", self.options.modelTextBinding);
			self.hiddenValueCtrl.attr("id", self.options.modelValueBinding);
			self.hiddenValueCtrl.attr("name", self.options.modelValueBinding);


			self.btn.on("click", self.getDropdownButtonHandler());
			self.textCtrl.on("click", self.getTextBoxClickHandler());
			self.textCtrl.on("change", self.getTextBoxChangeHandler());

			var dataList = null;

			if (self.options.globalListId) {
				dataList = $("#" + self.options.globalListId);
			}
			else {
				dataList = self.elem.find("dataList");
				dataList.remove();
			}

			self.itemList = dataList.data("json");	

	

			/*the jslint-exeption is for the "_" used by the jquery plugin on              
             the renderItem method that needs overridden. */
			
			/*jslint nomen:true*/
			var autocompleteObj = $(self.textCtrl).autocomplete({
				minLength: 0,
				source: self.itemList,
				
				select: function (event, ui) {
					/// <summary>
					/// jquery-ui widget select event hander
					/// </summary>
					/// <param name="event" type="Event"></param>
					/// <param name="ui"></param>

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
			//var hf = $(self.elem).next();
			//if (hf.length > 0) {
			//	this.hiddenField = hf;
			//	this.configureValuePost();
			//	this.registerBlurHandler();

			//}

			autocompleteObj.autocomplete("instance")._resizeMenu = function () {
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

			if (self.options.disabled && self.options.disabled === "disabled") {
				self.textCtrl.attr('disabled', 'disabled');
				self.btn.attr('disabled', 'disabled');
			}
		},
        setList: function(items) {
            /// <summary>
            /// Sets the list to use for the autocomplete dropdown.
            /// </summary>
            /// <param name="items" type="Array"></param>
            this.itemList = items;
            $(this.textCtrl).autocomplete("option", "source", items);
        },
		getDropdownButtonHandler: function () {
			var self = this;
			return function (e, ui) {
				e.preventDefault();
				e.stopPropagation();
				if (self.isOpen) {
					self.textCtrl.autocomplete("close");
					self.isOpen = false;
				} else {
					self.textCtrl.autocomplete("search", "");
					self.isOpen = true;
				}
				
				//self.textCtrl.autocomplete("close");
				return false;
			};
		},
		getTextBoxClickHandler:function(){			
			return function (e, ui) {
				$(this).select();
			};
		},
		getTextBoxChangeHandler:function(){
			var self = this;
			return function (e, ui) {
				var currentVal = $(this).val();

				var item = Enumerable.From(self.itemList)
					.FirstOrDefault(null, function (x) {
						return x.label === currentVal;
					});

				if (item !== null) {
					self.select(item);
				}
				else {
					self.hiddenTextCtrl.val(currentVal);
					self.clearSelectedItem();
					self.elem.trigger("selected", null);
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
		getSelectedItem: function () {
			return this.selectedItem;
		},
		clearSelectedItem: function () {			
			this.hiddenValueCtrl.val("");
			this.selectedItem = {};
		},
		select: function (item) {
			/// <summary>
			/// Selects the specified data item
			/// </summary>
			/// <param name="item">The data item to be selected</param>

			var self = this;

			$(self.textCtrl).val(item[self.options.displayMember])
                .data("itemId", item[self.options.idMember])
                .data("item", item);
			self.selectedItem = item;

			if (this.hiddenTextCtrl) {
				this.hiddenTextCtrl.val(item[self.options.displayMember]);
			}

			if (this.hiddenValueCtrl) {
				this.hiddenValueCtrl.val(item[self.options.idMember]);
			}
			self.elem.trigger("selected", item);
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


	$.fn.combobox = function (option, optionValue) {
		/// <summary>
		/// UL combobox widget with MVC html extension wrapper.
		/// c# Usage: @Html.Combobox("myinput", string.Empty)
		/// or: @Html.ComboboxFor(m=> m.MyProperty, "/profile/SearchULUsers")
		/// </summary>
		/// <param name="option"></param>
		/// <param name="optionValue"></param>
		/// <returns type=""></returns>

		var result = null;

		if (typeof option === 'string') {

			var amObj = $(this[0]).data("UL.Combobox");
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
				var ctrl = new UL.Combobox(elem, data);
				elem.data("UL.Combobox", ctrl);
				ctrl.init();
			});
		}

		return result || $(this);

	};

}(jQuery));