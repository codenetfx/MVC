/// <reference path="../_references.js" />
/// <reference path="../_ULReferences.js" />
/// <reference path="../../Lib/jquery/plugins/linq-vsdoc.js" />
/// <reference path="../../Lib/jquery/plugins/linq.js" />
/// <reference path="UL.Monitor.js" />

(function () {
    'use strict';
    if (!window.UL) {
        window.UL = {};
    }

    ///<field name="UL.NotificationStatus">Notification Status Enumeration</field>
    UL.NotificationStatus = {
        Undefined: 0,
        Critical: 1,
        Warning: 2,
        Informational: 3,
        Positive: 4
    };

    UL.Utility.safeFreeze(UL.NotificationStatus);

    UL.NotificationOptions = function () { return this; };
    UL.NotificationOptions.prototype = {
        ///<field name="selection">The elem.</field>    
        url: null,
        dismissUrl: null,
        pagerSelector: ".pager",
        itemTemplateSelector: '.item-template',
        entityViewButtonSelector: ".view-entity-btn",
        sortContainerSelector: ".sort-container",
        sortItemTemplateSelector: ".sort-item-template",
        containerSelector: ".container",
        taxonomyContainerSelector: ".notification-taxonomy",
        taxonomyItemTemplateSelector: ".taxonomy-item-template",
        entityContainerSelector: ".entity-taxonomy",
        entityItemTemplateSelector: ".entity-item-template",
        checkSelector: ".item-selector",
        refreshButtonSelector: ".refresh",
        enableEntityView: true,
        enableRefreshButton: false,
        progressAnimation: null
    };

    UL.Notifications = function (elem, options, proxy) {
        /// <summary>
        /// Provides a classifier for managing the notification widget data and user interaction.
        /// </summary>
        /// <param name="elem" type="jQuery">Target element.</param>
        /// <param name="options" type="UL.NotificationOptions">Configuration options</param>
        /// <param name="proxy" type="UL.Proxy">Server Proxy class</param>
        /// <returns type="UL.Notifications">Notification class instance.</returns>

        /// <field name="elem" type="jQuery">Target element.</field>
        /// <field name="opt" type="UL.NotificationOptions">The Notification control otpions.</field>
        /// <field name="proxy" type="UL.Proxy">Server Proxy class</field>
        /// <field name="proxyOptions" type="UL.ProxyOptions">The configuration options for using the UL.Proxy class.</field>
        /// <field name="container" type="jQuery">jQuery Ref to the Taxomomy container html tag.</field>
        /// <field name="taxonomyContainer" type="jQuery">jQuery Ref to the Taxomomy container html tag.</field>
        /// <field name="taxonomyItemTemplate" type="String">The templated html for the taxonomy Menu item</field>
        /// <field name="entityItemTemplate" type="String">The templated html for the entity taxonomy Menu item.</field>
        /// <field name="sortContainer" type="jQuery">jQuery Ref to the sort container.</field>
        /// <field name="sortItemTemplate" type="String">the templated html for a sort item.</field>
        /// <field name="itemTemplate" type="String">The templeted html for a notifiction item.</field>
        /// <field name="refreshButton" type="jQuery">jQuery Ref to the refresh button (this is mainly for debugging).</field>
        /// <field name="multiSelectMenu" type="jQuery">jQuery Ref to the multi select menu.</field>
        /// <field name="pagerContainer" type="jQuery">jQuery Ref to the container that holds the paging controls.</field>
        /// <field name="entityExtendedData" type="Object">Provides a dictionary lookup for Entity specific item extension information.</field>

        this.elem = elem;
        this.opt = new UL.NotificationOptions();
        this.proxy = proxy || new UL.Proxy();
        this.proxyOptions = null;

        //visual members
        this.container = null;
        this.taxonomyContainer = null;
        this.taxonomyItemTemplate = null;
        this.entityContainer = null;
        this.entityItemTemplate = null;
        this.sortContainer = null;
        this.sortItemTemplate = null;
        this.itemTemplate = null;
        this.refreshButton = null;
        this.multiSelectMenu = null;
        this.pagerContainer = null;

        //data members
        this.notificationList = null;
        this.taxonomyList = null;
        this.entityList = null;
        this.filters = null;
        this.request = null;
        this.entityExtendedData = null;

        $.extend(this.opt, this.elem.data());
        $.extend(this.opt, options);
        return this;
    };

    UL.Notifications.prototype = {
        init: function () {
            /// <summary>
            /// Initialize the Notifications widget control.
            /// </summary>          

            this.elem.hide();
            this.container = this.elem.find(this.opt.containerSelector);
            this.loadFilters();
            this.loadPager();
            this.loadItemTemplate();
            this.loadTaxonomyTemplate();
            this.loadEntityTemplate();
            this.loadSortTemplate();
            this.loadMultiSelectMenu();
            this.initRefreshButton();
            this.Refresh();
            this.elem.show();
        },

        loadEntityTemplate: function () {
            /// <summary>
            /// Private: loads the Notifiction Item Template.
            /// </summary>

            this.entityContainer = this.elem.find(this.opt.entityContainerSelector);
            var itemTemplate = this.entityContainer.find(this.opt.entityItemTemplateSelector).remove();
            this.entityItemTemplate = itemTemplate.htmlAll();
        },
        loadItemTemplate: function () {
            /// <summary>
            /// Private: loads the Notifiction Item Template.
            /// </summary>

            var templateObj = this.elem.find(this.opt.itemTemplateSelector);
            templateObj.remove();
            this.itemTemplate = templateObj.htmlAll();
        },
        loadPager: function () {
            this.pagerContainer = this.elem.find(this.opt.pagerSelector);
        },
        initRefreshButton: function () {
            var self = this;
            var rbtn = this.elem.find(this.opt.refreshButtonSelector);
            if (this.opt.enableRefreshButton) {
                this.refreshButton = rbtn.click(function (e, data) {
                    self.Refresh();
                });
            }
            else {
                rbtn.hide();
            }
        },
        loadTaxonomyTemplate: function () {
            this.taxonomyContainer = this.elem.find(this.opt.taxonomyContainerSelector);
            var itemTemplate = this.taxonomyContainer.find(this.opt.taxonomyItemTemplateSelector).remove();
            this.taxonomyItemTemplate = itemTemplate.htmlAll();
        },
        loadSortTemplate: function () {
            /// <summary>
            /// Loads the sort Item Template html.
            /// </summary>

            this.sortContainer = this.elem.find(this.opt.sortContainerSelector);
            var itemTemplate = this.sortContainer.find(this.opt.sortItemTemplateSelector).remove();

            this.sortItemTemplate = itemTemplate.htmlAll();
        },
        loadMultiSelectMenu: function () {

            this.multiSelectMenu = this.elem.find(".selection-pane").hide()
                .css("position", "absolute").css("top", "370px").css("right", "-23px");
            this.initializeMultiMenu();

        },
        loadFilters: function () {
            /// <summary>
            /// Private: Loads a reference to the jqResult containing 
            /// an htmlObject used to template the filters. it also initlizes 
            /// the request object containing the filter data.
            /// </summary>

            var self = this;
            var filterHolder = self.elem.find(".entity-filter");
            if (filterHolder.length > 0) {
                self.filters = filterHolder.data("raw");
            }

            if (!self.request) {
                self.request = {};
                self.request.Initialize = true;
                self.request.Criteria = {};
                self.request.Criteria.Filters = self.request;
            }
        },
        getProxyOptions: function () {
            /// <summary>
            /// Returns the Notifications current porxyOptions.
            /// </summary>
            /// <returns type="UL.ProxyOptions">the configured proxy options</returns>

            if (this.proxyOptions === null) {
                var po = new UL.ProxyOptions();
                po.blockedElement = this.elem;
                po.enableBlocking = true;
                po.transparentBlock = true;
                po.loadAnamationRef = UL.loadAnimationLarge;
                this.proxyOptions = po;
            }

            return this.proxyOptions;
        },
        Refresh: function () {
            /// <summary>
            /// Refreshes the data and the display of the Notification Widget.
            /// </summary>

            var self = this;
            self.request.Criteria.Filters = self.filters;


            this.proxy.send(self.request, self.opt.url,
                function (success, data, error) {

                    if (data && data.Successful) {
                        self.notificationList = data.Items;
                        self.request = data.Request;
                        self.Render(data);
                    } else {
                        if (data && !(data.Successful === undefined)) {
                            UL.HandleAjaxError(data);
                        } else {
                            UL.HandleAjaxUnhandledErrorContentAll(data);
                        }
                    }
                },
                self.getProxyOptions());
        },

        ConstituteJSonStrToObject: function (jsonString) {
            /// <summary>
            /// Converts the specified json string into an js object 
            /// reference representation of the data in the string.
            /// </summary>
            /// <param name="jsonString" type="String">The string to be converted.</param>
            /// <returns type="Object">The object reference.</returns>

            return jQuery.parseJSON(jsonString);
        },

        ClearNotificationContainer: function () {
            /// <summary>
            /// Clears all the notifications from the visual container.
            /// </summary>

            if (this.container !== null) {
                this.container.remove();
            }
        },
        Render: function (data) {
            /// <summary>
            /// Renders the specified data to the notification control.
            /// </summary>
            /// <param name="data" type="Object">That data to be rendered to the screen.</param>

            try {
                var self = this;
                self.renderPaging(data);
                self.renderEntityItems(data);
                self.renderTaxonomyItems(data);
                self.renderSortItems(data);
                self.renderNotificationItems(data);
            } catch (ignore) {

            }
        },

        renderNotificationItems: function (data) {
            /// <summary>
            /// Private: Generates the html needed to render a group of notification items.
            /// </summary>
            /// <param name="data">The data to be used for the rendering.</param>

            var self = this;

            self.container.empty();
            var tempContainer = self.container;
            var i = 0;
            var htmlItem = null;

            for (i = 0; i < data.Items.length; i++) {
                self.cleanNotificationItemData(data.Items[i]);
                htmlItem = self.mapItemToTemplate(self.itemTemplate, data.Items[i]);
                tempContainer.append(htmlItem);
            }

            self.initializeModalLinks();
            self.initializeDismissLinks();
            self.initilizeCheckboxes();
        },

        renderEntityItems: function (data) {

            var self = this;
            var items = data.EntityList;

            self.entityList = items;
            var container = self.entityContainer;
            var i = 0,
                htmlItem = null;

            container.empty();

            for (i = 0; i < items.length; i++) {
                htmlItem = $(self.mapItemToTemplate(self.entityItemTemplate, items[i]));
                container.append(htmlItem);

                if (items[i].Selected) {
                    htmlItem.addClass("disabled");
                }
            }


            self.entityContainer.find("li:not(.disabled) a").click(function (e) {
                e.preventDefault();
                if ($(this).hasClass(".disabled")) { return; }

                var btn = $(this);

                var key = btn.data("refinerKey").toString();

                if (self.request.SelectedEntity === null
                    || self.request.SelectedEntity.Key !== key) {
                    self.request.SelectedEntity = self.entityList
                       .find(function (x) { return x.Key === key; });


                    //reset sub refiner to "all" option
                    self.request.SelectedTaxonomy = self.taxonomyList
                        .find(function (x) { return x.Key === 0; });

                    self.request.Criteria.Paging.Page = 1;
                    self.Refresh();
                }

            });

        },
        renderTaxonomyItems: function (data) {
            /// <summary>
            /// Private: Creates the html elements needed to present Taxonomy Menu Items.
            /// </summary>
            /// <param name="data" type="Object">That Taxomomy menu item strcuture.</param>

            var self = this;
            var items = data.TaxonomyList;
            self.taxonomyList = items;
            var tempContainer = self.taxonomyContainer;
            var i = 0,
                htmlItem = null;

            tempContainer.empty();

            for (i = 0; i < items.length; i++) {
                htmlItem = $(self.mapItemToTemplate(self.taxonomyItemTemplate, items[i]));
                tempContainer.append(htmlItem);

                if (items[i].Selected) {
                    htmlItem.addClass("disabled");
                }
            }

            self.taxonomyContainer.find("li:not(.disabled) a").click(function (e) {
                e.preventDefault();
                var btn = $(this);

                var key = btn.data("refinerKey").toString();

                self.request.SelectedTaxonomy = self.taxonomyList
                    .find(function (x) { return x.Key === key; });

            	//Clearing Selected Entity.
                self.request.SelectedEntity = null;
                self.request.Criteria.Paging.Page = 1;
                self.Refresh();

            });
        },

        renderSortItems: function (data) {
            /// <summary>
            /// Private: Renders the sort menu.
            /// </summary>
            /// <param name="data">Data containing sort menu information.</param>

            var self = this,
                items = data.Request.Criteria.Sorts,
                tempContainer = self.sortContainer,
                i = 0,
                htmlItem = null;

            tempContainer.empty();

            for (i = 0; i < items.length; i++) {
                htmlItem = $(self.mapItemToTemplate(self.sortItemTemplate, items[i]));
                tempContainer.append(htmlItem);
                if (items[i].FieldName === self.request.CurrentSort.FieldName
                    && items[i].Order === self.request.CurrentSort.Order) {
                    htmlItem.addClass("disabled");
                }
            }

            self.sortContainer.find('li.disabled a').click(function (e) {
                e.preventDefault();
                return false;
            });

            self.sortContainer.find("li:not(.disabled) a").click(function (e) {
                e.preventDefault();
                var btn = $(this);
                var field = btn.data("sortfield");
                var order = btn.data("sortorder");

                self.request.CurrentSort = self.request.Criteria.Sorts
                    .find(function (x) { return x.FieldName === field && x.Order === order; });

                self.Refresh();

            });
        },

        renderPaging: function (data) {
            /// <summary>
            /// Private: Renders the paging control
            /// </summary>
            /// <param name="data"></param>

            var self = this;
            self.pagerContainer.empty();
            var pagerControl = self.pagerContainer.append(data.PagingHtmlString);

            pagerControl.find('button[name="Paging.Page"]').click(function (e) {
                e.preventDefault();
                var btn = $(this);
                var pageValue = btn.val();
                self.request.Criteria.Paging.Page = pageValue;
                self.Refresh();
            });
        },

        mapItemToTemplate: function (templateHtmlStr, item) {
            /// <summary>
            /// Private: Renders an html object using the specified template and itme.
            /// </summary>
            /// <param name="templateHtmlStr" type="String">The html template in a string.</param>
            /// <param name="item" type="Object">the data item.</param>
            /// <returns type="HtmlObject">Returns the render html.</returns>

            return UL.TemplateEngine(templateHtmlStr, item);
        },

        cleanNotificationItemData: function (item) {
            /// <summary>
            /// Private: Enhances the display of the specified item with 
            /// visual effects based on notifiation status.
            /// </summary>
            /// <param name="item"></param>

            var self = this;

            if (item.Body !== null) {
                item.Body = self.ConstituteJSonStrToObject(item.Body);
            }
        },
        SendDismissals: function (idList) {
            /// <summary>
            /// Sends a request to the server to dismiss any notification with a matching id in the specified list.
            /// </summary>
            /// <param name="idList" type="Guid[]">List of notification ids to be dismissed.</param>

            var self = this;

            this.proxy.send({ Ids: idList }, self.opt.dismissUrl,
                function (success, data, error) {

                    if (data !== null && data.Successful) {

                        $.each(idList, function (index, value) {
                            $('#nItem_' + value).remove();
                            self.notificationList.remove(function (x) { return x.Id === value; });
                        });

                        self.hideMultiSelectMenu();

                        self.Refresh();
                    } else {
                        if (data && !data.Successful) {
                            UL.HandleAjaxError(data);
                        }
                    }
                },
                self.getProxyOptions());

        },
        initializeDismissLinks: function () {
            /// <summary>
            /// Private: Attaches a click handler to all of the dismiss links.
            /// </summary>

            var self = this;

            $('.dismiss-notification').click(function (e) {
                e.preventDefault();
                var id = $(this).data("id");
                var ids = new Array(id);
                self.SendDismissals(ids);
                return false;
            });
        },

        initializeViewLinks: function () {
            /// <summary>
            /// Private: Attaches a click handler to view links.
            /// </summary>

            var self = this;
            $('.notification-action-item').click(function (e) {
                e.preventDefault();
                var url = $(this).attr('href');
                var nId = $(this).data("id");
                // window.location = url + "/" + nId;
                var notification = self.notificationList.find(function (x) { return x.Id === nId; });
                $.get(url, notification, function (data) {
                    window.location = data;
                });
            });
        },
        initializeModalLinks: function () {
            /// <summary>
            /// Private: Attaches click handler to show a model to all links configured for a modal.
            /// </summary>            
            this.container.find('[data-toggle="modal"]').click(UL.ModelLinkHandler);
        },
        initilizeCheckboxes: function () {
            /// <summary>
            /// Private: Attaches the change event to all check boxes.
            /// </summary>

            var self = this;
            self.container.find(self.opt.checkSelector).change(function (e) {
                var chks = self.container.find(self.opt.checkSelector + ':checked');
                self.setMenuSelectedCount(chks.length);
                if (chks.length > 0) {
                    self.showMultiSelectMenu();
                }
                else {
                    self.hideMultiSelectMenu();
                }
            });
        },
        initializeMultiMenu: function () {
            /// <summary>
            /// private: Attaches a click handler to the links in the multi select menu.
            /// </summary>

            var self = this;
            self.multiSelectMenu.find("a.dismiss-multi").click(function (e) {
                e.preventDefault();
                var chks = self.container.find(self.opt.checkSelector + ':checked');
                var ids = [],
                    i = 0;

                for (i = 0; i < chks.length; i++) {
                    ids.push($(chks[i]).data("id"));
                }

                self.SendDismissals(ids);
            });
        },
        showMultiSelectMenu: function () {
            /// <summary>
            /// Private: Presensts the multi select action menu to the user.
            /// </summary>

            var self = this;
            if (!self.multiSelectMenu.is(":visible")) {
                self.multiSelectMenu.show();
            }

        },
        hideMultiSelectMenu: function () {
            /// <summary>
            /// Private Hides the Multi select action menu from the user.
            /// </summary>

            var self = this;
            if (self.multiSelectMenu.is(":visible")) {
                self.multiSelectMenu.hide();
            }
        },
        setMenuSelectedCount: function (count) {
            /// <summary>
            /// Private: Updates the selected count display 
            /// on the Multi select action menu.
            /// </summary>
            /// <param name="count"></param>

            var self = this;
            self.multiSelectMenu
                .find(".selected-count").text(count);
        }
    };

    var nm_Pad_Lock = {};
    UL.Utility.safeFreeze(nm_Pad_Lock);
    UL.Notifications.DataKey = "nmObj";

    //jquery plugin to api for notifications
    $.fn.notification = function (options, optionValue) {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="options"></param>
        /// <param name="optionValue"></param>
        /// <returns type=""></returns>

        UL.Monitor.Enter(nm_Pad_Lock, 2000, function (self, options, optValue) {
            var widgetObj = null;

            if (typeof options === 'string') {

                widgetObj = $(self[0]).data(UL.Notifications.DataKey);
                if (widgetObj !== null) {
                    try {
                        widgetObj[options](optionValue);
                    }
                    catch (e) {
                        throw options.toString() + " was an invalid behavior.";
                    }
                }
            }
            else {

                $(self).each(function (index, $elem) {
                    var elem = $($elem);
                    if (elem.data()[UL.Notifications.DataKey] === undefined) {
                        widgetObj = new UL.Notifications(elem, null, new UL.Proxy());
                        widgetObj.init();
                        elem.data(UL.Notifications.DataKey, widgetObj);
                    }
                });
            }
        }, [this, options, optionValue]);

        return $(this);
    };


}(jQuery));


