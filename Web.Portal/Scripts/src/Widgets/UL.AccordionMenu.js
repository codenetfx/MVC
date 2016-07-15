/// <reference path="../_references.js" />
/// <reference path="../_ULReferences.js" />
/// <reference path="UL.AutoComplete.js" />
/// <reference path="../../Lib/jquery/plugins/date.js" />
/// <reference path="../../Lib/jquery/plugins/moment.js" />

(function ($) {
	'use strict';
	if (!window.UL) {
		window.UL = {};
	}

	UL.AccordionMenuOptions = function () {
		this.maxRefiners = 10;
	};


	UL.AccordionMenu = function (elem, initOptions, providedProxy, criteria) {
		/// <summary>
		/// Provides a class for managing and creates an Accordien Menu
		/// </summary>
		/// <param name="elem"></param>
		/// <param name="initOptions" type="UL.AccordionMenuOptions" optional="true">Proivdes a options object for the AccordionMenu to use</param>
		/// <param name="providedProxy" type="UL.Proxy" optional="true">Provides an instance of a proxy class for communication to the server</param>
		/// <param name="criteria" type="SearchCriteria" optional="true">Provides a copy of the criteria class that will be manipulated and sent to the server for a page refresh.</param>
		this.element = $(elem);
		this.navData = null;
		this.criteria = criteria;
		var opt = (initOptions && (initOptions instanceof UL.AccordionMenuOptions))
            ? initOptions : new UL.AccordionMenuOptions();
		var data = this.element.data();
		$.extend(opt, data);

		var proxy = (providedProxy && providedProxy !== null)
            ? providedProxy : new UL.Proxy();

		this.getOptions = function () {
			/// <summary>
			/// Reterns a muted copy of the initialization options.
			/// </summary>
			/// <returns type="UL.ProxyOptions"></returns>

			//this access model to the opt value should be setup as a get/set 
			//property but it is not supported by ie8
			return opt; //.deepCopy();
		};

		this.getProxy = function () {
			return proxy;
		};

	};

	UL.AccordionMenu.prototype = {
		init: function () {
			var self = this;
			var proxy = this.getProxy();
			var options = this.getOptions();

			if (options.url && !options.preloadedRoot) {
				proxy.send(null, options.url, function (successful, data, error) {
					if (data !== null) {
						self.navData = data;
						self.render();
					}
				});
			}
			else {
				self.render();
			}
		},
		render: function () {
			var self = this;
			this.element.hide();
			this.buildPrimaryHtml(this.navData);

			this.element.accordion({
				collapsible: true,
				heightStyle: "content",
				icons: {
					"header": "ui-icon-plus", "activeHeader": "ui-icon-minus"
				},
				beforeActivate: self.getAccordionBeforeActivateHandler()
			});

			var selectedIndex = this.element.find('h3[data-selected="true"]').index();
			if (selectedIndex > 0) {
				var index = Math.ceil(selectedIndex / 2);
				this.element.accordion({ active: index });
			}

			this.element.show();
		},
		getAccordionBeforeActivateHandler: function () {
			/// <summary>
			/// Returns a function handler for  jqueryui accordion 
			/// controls beforeActivate event.
			/// </summary>

			var self = this;

			return function (event, ui) {
				///This is a temporary hack for postback
				//this should be removed when site is moved to RIA model
				var activeItemClicked = (ui.newHeader.length <= 0);
				var evalHeader = (!activeItemClicked) ? ui.newHeader : ui.oldHeader;
				var evalPanel = (!activeItemClicked) ? ui.newPanel : ui.oldPanel;
				var link = evalHeader.find('a');
				var hasLink = (link.length > 0);
				var allowDefault = !(evalHeader.hasClass("ui-no-children") || hasLink);
				if (hasLink) {
					window.location = link.prop("href");
				}

				if (allowDefault) {
					//this part would need to 
					//stay after above hack is removed
					var itemData = evalHeader.data();
					if (itemData) {
						var childUrl = itemData.DataUrl || itemData.dataUrl;
						if (childUrl) {
							var proxyOptions = new UL.ProxyOptions();
							proxyOptions.blockedElement = self.element;
							proxyOptions.transparentBlock = true;
							self.getProxy().send(self.criteria, childUrl, function (successful, dataRef, errorDetails) {

								if (successful) {
									evalHeader.data("item", {
										Children: dataRef
									});

									self.renderTree(evalHeader, evalPanel);
								}


								self.element.trigger('loadComplete.acdMenu', {});
							},
                            proxyOptions);
						}
					}
				}
				else {
					var activeClasses = 'ui-accordion-header-active ui-state-active';
					evalHeader.addClass(activeClasses);
					if (ui.oldHeader !== null && ui.oldHeader !== evalHeader) {
						ui.oldHeader.removeClass(activeClasses);
					}
				}

				return allowDefault;
			};
		},
		toggleItemSelect: function (menuItem) {
			var refiner = this.element.find('li[data-jqkey="' + menuItem.Key + '"][data-jqvalue="' + menuItem.RefinementValue + '"]');
			if (!refiner.hasClass(this.getActiveClass())) {
				this.selectItem(menuItem);
			}
			else {
				this.deselectItem(menuItem);
			}
		},
		getActiveClass: function () {
			return "ui-state-active-leaf";
		},
		selectItem: function (menuItem) {

			if (menuItem) {
				var activeClass = this.getActiveClass();
				var refinerList = this.element.find('li[data-jqkey="' + menuItem.Key + '"]');
				var allRefiner = this.element.find('[data-jqkey="' + menuItem.Key + '"][data-jqvalue="All"]');

				//all option
				if (!menuItem.RefinementValue || menuItem.RefinementValue === "All") {
					refinerList.removeClass(activeClass);
					allRefiner.addClass(activeClass);
				}
				else {

					if (allRefiner.hasClass(activeClass)) {
						allRefiner.removeClass(activeClass);
					}

					var refiner = this.element.find('li[data-jqkey="' + menuItem.Key + '"][data-jqvalue="' + menuItem.RefinementValue + '"]');
					refiner.addClass(activeClass);
				}
			}
		},
		getMenuItem: function (filterItem) {
			return this.element.find('li[data-jqkey="' + filterItem.Key + '"][data-jqvalue="' + filterItem.RefinementValue + '"]')
                .data("item");
		},
		deselectItem: function (menuItem) {

			if (menuItem) {
				var activeClass = this.getActiveClass();
				var allRefiner = this.element.find('li[data-jqkey="' + menuItem.Key + '"][data-jqvalue="All"]');

				//all option
				if (menuItem.RefinementValue && menuItem.RefinementValue !== "All") {
					var refiner = this.element.find('li[data-jqkey="' + menuItem.Key + '"][data-jqvalue="' + menuItem.RefinementValue + '"]');
					refiner.removeClass(activeClass);

					if (refiner.parent().find('.' + activeClass).length <= 0) {
						allRefiner.addClass(activeClass);
					}
				}
			}
		},
		lessIsMore: function () {
			var maxRefiners = this.getOptions().maxRefiners;
			var menus = this.element.find('ul.menu-item-refiner');
			var kids;
			var moreTag;
			Enumerable.From(menus).ForEach(function (value) {
				kids = $(value).children('li');
				if (kids.length > maxRefiners) {
					kids.slice(maxRefiners + 1, kids.length).addClass('hide');
					moreTag = UL.Utility.createTag('li');
					moreTag.addClass("more-link")
                        .append(UL.Utility.createTag('span').text("more...")).click(function (e) {
                        	e.preventDefault();
                        	$(this).closest('UL').toggleClass('show-more');
                        	if ($(this).find('span').text() === 'more...') {
                        		$(this).find('span').text('less...');
                        	} else {
                        		$(this).find('span').text('more...');
                        	}
                        	return false;
                        });
					$(value).append(moreTag);
				}
			});
		},
		renderTree: function (header, panel) {
			/// <summary>
			/// 
			/// </summary>
			/// <param name="header"></param>
			/// <param name="panel"></param>
			/// 
			var self = this;
			panel.empty();
			var menuItem = header.data().item;
			var ul = UL.Utility.createTag('ul');

			panel.append(ul);

			var treeCrawler = function (parentNode, item) {

				var children = item.Children || null;
				var countLabel = "";
				if (item.Count && item.Count > 0 && (!children || children.length === 0)) {
					countLabel = " (" + item.Count + ")";
				}

				var refineValue = item.RefinementValue;
				var activeClass = "";

				if (item.Text === "All") {
					refineValue = "All";
					activeClass = self.getActiveClass();
					item.RefinementValue = "All";
				}

				var li = UL.Utility.createTag('li')
                    .append(UL.Utility.createTag('span')
                        .text(item.Text + countLabel))
                        .prop("title", item.ToolTip || '')
                        .css('cursor', 'pointer')
                        .data('item', item)
                        .attr("data-jqkey", (item.RefinementValue) ? item.Key : "")
                        .attr("data-jqvalue", refineValue)
                        .addClass(activeClass)
                        .addClass(item.ControlledEventClass || "default-toggle");


				parentNode.append(li);

				if (children && children.length > 0) {
					var ultag = UL.Utility.createTag('ul');
					ultag.addClass('menu-item-refiner');
					li.append(UL.Utility.createTag('div').append(ultag));

					Enumerable.From(children).ForEach(function (child) {
						treeCrawler(ultag, child);
					});
				}
			};

			var createAutoCompleteControl = function (displayDetails, allLinkActiveClass, filterOverrideText) {


				var autoCompleteCtrl = UL.Utility.createTag("input")
			        .attr("data-url", displayDetails.Url)
			        .attr("data-description-member", displayDetails.DescriptionMember)
			        .attr("data-display-member", displayDetails.DisplayMember)
			        .attr("data-id-member", displayDetails.IdMember)
			        .attr("data-max-scroll-items", displayDetails.MaxScrollItems)
			        .attr("data-min-term-length", displayDetails.MinTermLength)
			        .attr("data-post-value", false)
			        .attr("data-val", false)
			        .ulAutoComplete()
			        .ulAutoComplete("registerSelectEventHandler", function (event, ui) {
			        	var parentMenuItem = $($(this).parents("li")[0]).data("item");
			        	var ctrlConfig = $(this).data();
			        	var selectedItem = $(this).ulAutoComplete("getSelectedItem");
			        	var itemTemplate = UL.Utility.deepCopy(parentMenuItem);
			        	itemTemplate.RefinementValue = selectedItem[ctrlConfig.idMember];
			        	itemTemplate.Text = selectedItem[ctrlConfig.displayMember];
			        	if (filterOverrideText) {
					        parentMenuItem.Text = filterOverrideText;
				        }
			        	
			        	self.element.trigger('leafClicked.acdMenu', {
			        		elem: $(this),
			        		menuItem: itemTemplate,
			        		parentItem: parentMenuItem
			        	});

			        	var allLink = $(this).prev("a");
			        	allLink.removeClass(allLinkActiveClass);
			        	$(this).val("");
			        })
			        .on("keydown", function (e, ui) {
			        	var code = e.keyCode || e.which;
			        	switch (code) {
			        		case 13:
			        			if ($(this).val().trim() !== "") {
			        				$(this).ulAutoComplete("selectIndex", 0);
			        				$(this).val("");
			        				$(this).autocomplete("close");
			        			}
			        			break;
			        		case 27:
			        			$(this).val("");
			        			break;
			        	}

			        });
				return autoCompleteCtrl;

			};
			var renderAutoComplete = function (parentNode, item, displayDetails) {

				var allLinkActiveClass = "ui-state-active-all";
				var li = UL.Utility.createTag('li')
                    .append(UL.Utility.createTag('span')
                        .text(item.Text))
                        .prop("title", item.ToolTip || '')
                        .css('cursor', 'pointer')
                        .data('item', item)
                        .attr("data-jqkey", (item.RefinementValue) ? item.Key : "")
                        .attr("data-jqvalue", item.RefinementValue);


				parentNode.append(li);

				var autoCompleteCtrl = createAutoCompleteControl(displayDetails, allLinkActiveClass);

				var allItem = UL.Utility.deepCopy(item);
				allItem.RefinementValue = "All";
				allItem.Text = "All";

				var allLink = UL.Utility.createTag("a")
                    .data("item", allItem)
                    .addClass("all-link " + allLinkActiveClass)
                    .append(
                        UL.Utility.createTag("span")
                            .text(" (All)")
                    )
                    .on("click", function (e, ui) {
                    	e.preventDefault();
                    	var parentMenuItem = $($(this).parents("li")[0]).data("item");
                    	self.element.trigger('leafClicked.acdMenu', {
                    		elem: $(this),
                    		menuItem: $(this).data("item"),
                    		parentItem: parentMenuItem
                    	});

                    	if (!$(this).hasClass(allLinkActiveClass)) {
                    		$(this).addClass(allLinkActiveClass);
                    	}
                    });

				li.append(allLink);
				li.append(autoCompleteCtrl);
			};

			var renderRefinerWithAutoCompleteChild = function (parentNode, item, displayDetails) {
				var allLinkActiveClass = "ui-state-active-all";
				var pickOptions = displayDetails.PickOptionDisplayValues;

				Enumerable.From(pickOptions).ForEach(function (pickOption) {
					pickOption.RefinementValue = pickOption.ControlFlag;
					pickOption.ControlledEventClass = "nav-autocomplete-item";
					item.Children.push(pickOption);
				});

				treeCrawler(parentNode, item);

				$(panel).find('li.nav-autocomplete-item').each(function(index, elem) {
					var autoCompleteCtrl = createAutoCompleteControl(pickOptions[index], allLinkActiveClass, pickOptions[index].FilterText);
					$(elem)
					.append(autoCompleteCtrl)
            			.click(function (e, ui) {
            				e.preventDefault();
            				e.stopPropagation();
            			});

				});

				return;
			};

			var renderDatePicker = function (parentNode, item, displayDetails) {

				var pickOptions = displayDetails.PickOptionDisplayValues;

				Enumerable.From(pickOptions).ForEach(function (pickOption) {
					pickOption.RefinementValue = pickOption.ControlFlag;
					pickOption.ControlledEventClass = "nav-date-picker";
					item.Children.push(pickOption);
				});

				treeCrawler(parentNode, item);

				$(panel).find('li.nav-date-picker').click(function (e, ui) {
					e.preventDefault();
					e.stopPropagation();

					var elem = $(this);
					var data = elem.data();
					var parent = elem.parents("li");
					var parentData = parent.data();
					var parentMenuItem = parentData ? parentData.item : null;


					if (!data.hasDatePicker) {
						data.hasDatePicker = true;
						elem.datepicker({
							todayBtn: true
						}).datepicker("setDate", Date.now())
                        .bind("changeDate", function (e) {

                        	$(this).datepicker("hide");
                        	var itemObj = UL.Utility.deepCopy(data.item);
                        	var datePicked = moment(e.date).format("YYYY-MM-DD");
                        	itemObj.RefinementValue = String.format(itemObj.Format, [datePicked, itemObj.ControlFlag, itemObj.Text]);
                        	itemObj.Text = itemObj.Text + ": " + datePicked;

                        	self.element.trigger('leafClicked.acdMenu', {
                        		elem: elem,
                        		menuItem: itemObj,
                        		parentItem: parentMenuItem
                        	});
                        });

						elem.datepicker("show");
					}

				});

				return;
			};

			var rootChildren = menuItem.Children || null;
			Enumerable.From(rootChildren).ForEach(function (child) {

				var displayDetails = (self.criteria.RefinerDetails !== undefined && self.criteria.RefinerDetails !== null)
                    ? self.criteria.RefinerDetails[child.Key]
                    : "";

				if (displayDetails !== undefined) {
					switch (displayDetails.DisplayType) {
						case "AutoComplete":
							renderAutoComplete(ul, child, displayDetails);
							break;
						case "DatePicker":
							renderDatePicker(ul, child, displayDetails);
							break;

						case "AutoCompleteItem":
							renderRefinerWithAutoCompleteChild(ul, child, displayDetails);
							break;

						default:
							treeCrawler(ul, child);
							break;
					}

				}
				else {
					treeCrawler(ul, child);
				}


			});

			$(panel).find('li.default-toggle').click(this.getTreeItemClickHandler());
			this.lessIsMore();

			//If there is only one refiner, we have to hide refiner name
			if (ul.children('li').length === 1) {
				var spanchild = ul.children('li').children('span');
				if (spanchild) {
					spanchild.hide();
				}
			}
		},
		nodeClicked: function (handler) {
			/// <summary>
			/// Registers the specified handler for the nodeClicked Event
			/// </summary>
			/// <param name="handler" type="Function">The event handler to be registered.</param>

			this.element.on("nodeClicked.acdMenu", handler);
		},
		leafClicked: function (handler) {
			/// <summary>
			/// Registers the specified handler for the nodeClicked Event
			/// </summary>
			/// <param name="handler" type="Function">The event handler to be registered.</param>

			this.element.on("leafClicked.acdMenu", handler);
		},
		loadComplete: function (handler) {
			/// <summary>
			/// Registers the specified handler for the nodeClicked Event
			/// </summary>
			/// <param name="handler" type="Function">The event handler to be registered.</param>

			this.element.on("loadComplete.acdMenu", handler);
		},
		getTreeItemClickHandler: function () {
			var self = this;
			return function (e) {
				e.preventDefault();
				var elem = $(this);
				var parent = elem.parents("li");
				var parentData = parent.data();
				var parentMenuItem = parentData ? parentData.item : null;

				var data = elem.data();
				if (data.item.Children && data.item.Children.length > 0) {
					var childContainer = elem.children('div');
					if (!childContainer.is(":visible")) {
						childContainer.show();
					}
					else {
						childContainer.hide();
					}
				}
				else {
					self.toggleItemSelect(data.item);
					self.element.trigger('leafClicked.acdMenu', {
						elem: elem,
						menuItem: data.item,
						parentItem: parentMenuItem
					});
				}

				self.element.trigger('nodeClicked.acdMenu', {
					elem: elem,
					menuItem: data,
					parentItem: parentMenuItem
				});

				return false;
			};
		},
		buildPrimaryHtml: function (data) {
			var self = this;

			if (data) {
				Enumerable.From(data).ForEach(function (menuItem) {

					var heading = UL.Utility.createTag("h3")
                        .data("key", menuItem.Key)
                        .data("item", menuItem);

					var container = UL.Utility.createTag("div")
                        .data("key", menuItem.Key);

					if (menuItem.Url && menuItem.Url !== "" && !menuItem.Selected) {
						heading.append(UL.Utility.createTag("a")
                            .prop("href", menuItem.Url)
                            .text(menuItem.Text))
                        .addClass("ui-no-children");
					}
					else {
						heading.text(menuItem.Text);
					}

					self.element.append(heading)
                        .append(container);


				});
			}
		}
	};


	$.fn.accordionMenu = function (options, optionValue) {

		if (typeof options === 'string') {

			var amObj = $(this[0]).data("UL.AccordionMenu");
			if (amObj !== null) {
				try {
					return amObj[options](optionValue);
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
				var criteriaTag = $('#defaultCriteria').data();
				var criteriaJson = null;

				if (criteriaTag) {
					criteriaJson = criteriaTag.json;
				}
				var menu = new UL.AccordionMenu(elem, null, new UL.Proxy(), criteriaJson);
				elem.data("UL.AccordionMenu", menu);
				menu.init();
			});
		}

		return $(this);
	};



}(jQuery));





