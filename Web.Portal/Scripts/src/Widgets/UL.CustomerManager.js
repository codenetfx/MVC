/// <reference path="../_references.js" />
/// <reference path="../_ULReferences.js" />
/// <reference path="../../Lib/jquery/plugins/linq-vsdoc.js" />
/// <reference path="../../Lib/jquery/plugins/linq.js" />
/// <reference path="UL.Monitor.js" />

//Customer lookup manager
(function ($) {
    'use strict';
    if (!window.UL) {
        window.UL = {};
    }

    var clm_Pad_Lock = {};
    UL.Utility.safeFreeze(clm_Pad_Lock);
    $.fn.customerlookupWidget = function (options, optionValue) {
        /// <summary>
        /// Initilizes Customer Lookup widget or calls UL.CustomerManager api method.
        /// </summary>
        /// <param name="options" type="Object/MethodNameString">init configuration or method name for post init calls.</param>
        /// <param name="optionValue" type="Object">Api method call parameter</param>
        /// <returns type="jQuery">jQuery Result reference.</returns>
       
        //var self = this;

        UL.Monitor.Enter(clm_Pad_Lock, 2000, function (self, options, optValue) {
            var clm = null;

            if (typeof options === 'string') {

                clm = $(self[0]).data(UL.CustomerManager.DataKey);
                if (clm !== null) {
                    try {
                        clm[options](optionValue);
                    }
                    catch (e) {
                        throw options.toString() + " was an invalid behavior.";
                    }
                }
            }
            else {

                $(self).each(function (index, $elem) {
                    var elem = $($elem);
                    if (elem.data()[UL.CustomerManager.DataKey] === undefined) {
                        clm = new UL.CustomerManager(elem, options, new UL.Proxy());
                        elem.data(UL.CustomerManager.DataKey, clm);
                        clm.init();
                    }
                });
            }
        }, [this,options, optionValue]);
              
        return $(this);
    };

    var Screen = function () {
        /// <summary>
        /// Provides a class for the Screen object represents of one step/panel in a wizard dialog.
        /// </summary>
        ///<field name="screenId" type="Int">The unique identifier for the screen.</field>      
        ///<field name="itemContainer" type="jqResult">the container to which items are added.</field>
        ///<field name="itemTemplateHtml" type="String">The item template for this Screen</field>
        ///<field name="screenRef" type="jqResult">a jquery result reference containing one screen html element refernece.</field>

        this.screenId = null;
        this.itemContainer = null;
        this.itemTemplateHtml = null;
        this.screenRef = null;
    };

    Screen.prototype = {

        clearItems: function () {
            /// <summary>
            /// clears items from the item container of this instance of the Screen.
            /// </summary>

            this.itemContainer.empty();
        },
        refresh: function (data) {
            /// <summary>
            /// Redraws the screen using the specified data.
            /// </summary>
            /// <param name="data">Data to use when refreshing the screen.</param>

            if (data && data.length > 0) {
                this.clearItems();
                var i, dataItem, mappedRow;

                for (i = 0; i < data.length; i++) {
                    dataItem = data[i];
                    mappedRow = UL.TemplateEngine(this.itemTemplateHtml, dataItem);
                    this.itemContainer.append(mappedRow);
                }
            }
        }
    };


    UL.CustomerManager = function (element, options, ulProxy ) {
        /// <summary>
        /// provides a class to manager Customer and agent data synconization for a project.
        /// </summary>
        /// <param name="element" type="jQuery">The jQuery Reuslt element to which the CustomerManager will attach itself.</param>
        /// <param name="options" type="Object">Configuration objects.</param>
        /// <param name="ulProxy" type="UL.Proxy">the ajax proxy class to use for request to the server.</param>

        ///<field name="oracleIdSource" type="jQuery">The oracle sourceId textbox</field>

        this.opt = options || {};
        this.element = element;
        this.proxy = ulProxy;
        this.proxyOptions = new UL.ProxyOptions();
        this.proxyOptions.enableBlocking = false; //set to false in order to handle manually.
        this.proxyOptions.enableKo = false;
        this.proxyOptions.enableLoadAnamation = true;
        this.proxyOptions.transparentBlock = true;
        this.proxyOptions.blockedElement = this.element;
        this.section = $(element);
        this.isInit = false;
        this.accountList = [];
        this.contactList = [];
        this.screens = [];
        this.syncBtn = null;
        this.url = null;
        this.modalBaseContent = null;
        this.modelRef = null;
        this.progressAnimation = null;
        this.fixedAccountId = null;
        this.isLineSelected = false;
        this.isFixedAccount = false;
        this.oracleIdSource = null;
        this.pad_lock = {};
        this.state = {
            companyId: null,
            accountNumber: null,
            accountId: null,
            account: null,
            contactId: null,
            contact: null,
            currentScreen: null
        };

        var data = this.element.data();
        $.extend(this.opt, data);

        this.getProxy = function () {
            return this.proxy;
        };
    };

    ///<field name="UL.CustomerManager.Datakey" type="String">the const string key used to attach 
    /// the customer manager to an html element.</field>
    UL.CustomerManager.DataKey = "clm";

    ///<field name="UL.CustomerManager.ScreenTypes" type="String">the Screen Type enumeration.</field>
    UL.CustomerManager.ScreenTypes = {
        ///<field name="Accounts" type="String">Account Screen = 1</field>
        Accounts: 1,
        ///<field name="Accounts" type="String">Contact Screen = 2</field>
        Contacts: 2,
        getName: function (intVal) {
            /// <summary>
            /// Gets the Enum type name matching the specified typed enum int value
            /// </summary>
            /// <param name="intVal" type="Int"></param>
            /// <returns type="String">The enum Type name.</returns>
            var self = this;
            var foundMember = null;

            Enumerable.From(self).ForEach(function (member) {
                if (typeof self[member.Key] !== 'Function') {
                    if (self[member.Key] === intVal) {
                        foundMember = member.Key;
                        return;
                    }
                }
            });

            return foundMember;
        },
        getVal: function (strName) {
            /// <summary>
            /// Returns the enum type int value for the specified enum type string.
            /// </summary>
            /// <param name="strName" type="String">Enum Type name as a string.</param>
            /// <returns type="Int">The int enum type value</returns>

            try {
                return UL.CustomerManager.ScreenTypes[strName];
            }
            catch (e) {
                return null;
            }
        }
    };


    UL.CustomerManager.prototype = {

        init: function () {
            /// <summary>
            /// Private: initializes an instance of the CustomerManager Object.
            /// </summary>

            var self = this;
            self.modalBaseContent = self.section.find(self.section.data("modalSelector"));
            self.modalBaseContent.remove();
            self.modalBaseContent.show();
            self.fixedAccountId = self.section.data("fixedAccountId");
            self.isFixedAccount = (self.fixedAccountId && self.fixedAccountId > 0);
            self.oracleIdSource = $(self.section.data("variableAccountIdSource"));


            self.isLineSelected = self.section.data("selectedLines");

            self.url = self.section.data("url");
            self.screens = this.loadScreens(self.section.data("screenSelector"));
            self.syncBtn = self.section.find(self.section.data("syncButtonSelector"));
            this.registerLookupEvent();
            this.state.currentScreen = this.getScreen(UL.CustomerManager.ScreenTypes.Accounts);
            self.state.companyId = self.section.data("companyId");
            self.state.accountNumber = function () { return self.getOracleId(); };

            //init loading animation
            self.progressAnimation = $($.fn.modal.defaults.spinner).css("position", "relative")
                .css("zindex", "1000").css("top", "-250px");
            self.progressAnimation.find(".message").css("background-color", "white");

            this.isInit = true;
        },

        showScreen: function (screen, data) {
            /// <summary>
            /// Displays the specified screen to the using.
            /// </summary>
            /// <param name="screen">The screen to display.</param>
            /// <param name="data">The data to be rended into the screen.</param>

            var self = this;
            if (screen !== null) {
                screen.refresh(data);
                var modal = self.getModal();
                var canvas = modal.find(".screen-canvas").empty();
                canvas.append(screen.screenRef);
                self.initScreenForDisplay(screen);
            }
        },

        initScreenForDisplay: function (screen) {
            /// <summary>
            /// Private: Initilizes the specified screen.
            /// </summary>
            /// <param name="screen">The screen to be initialized.</param>

            var self = this;
            var screenId = UL.CustomerManager.ScreenTypes.getVal(screen.screenId);
            switch (screenId) {
                case UL.CustomerManager.ScreenTypes.Accounts:
                    screen.screenRef.find('input[type="radio"]').change(function (e) {

                        var accountId = $(this).data("id");
                        self.state.account = self.accountList.find(function (x) { return x.RuntimeId === accountId; });
                        self.state.accountId = self.state.account.Id;
                        self.modelRef.find(".next-btn").show().prop("disabled", false);

                    });
                    if (self.modelRef) {
                        self.modelRef.find(".next-btn").show().prop("disabled", true);
                        self.modelRef.find(".back-btn").hide();
                        self.modelRef.find(".done-btn").hide();
                    }

                    break;
                case UL.CustomerManager.ScreenTypes.Contacts:
                    screen.screenRef.find('input[type="radio"]').change(function (e) {
                        var contactId = $(this).data("id");
                        self.state.contactId = contactId;
                        self.state.contact = self.contactList.find(function (x) { return x.Id === contactId; });
                        self.modelRef.find(".done-btn").show().prop("disabled", false);
                    });
                    if (self.modelRef) {
                        if (!self.isFixedAccount) {
                            self.modelRef.find(".back-btn").show();
                        }
                        else {
                            self.modelRef.find(".back-btn").hide();
                        }

                        self.modelRef.find(".next-btn").hide();
                        self.modelRef.find(".done-btn").show().prop("disabled", true);
                    }
                    break;
            }
        },
        getOracleId: function () {
            var self = this;

            if (this.isFixedAccount) {
                return this.fixedAccountId;
            }

            return self.getOracleSourceId();
        },
        getOracleSourceId: function(){
            var self = this;
            if (self.oracleIdSource) {
                return self.oracleIdSource.val();
            }
            
            return null;
        },
        getAccountIds: function() {
            var self = this;
            var url = self.section.data("url");
            var companyId = self.state.companyId;
            var deferred = $.Deferred();
            var accountId = self.state.accountNumber();

            if (accountId === undefined) {
                self.getProxy().send({ CompanyId: companyId }, url, function(successful, response, errors) {
                        deferred.resolve(response);
                    }, self.proxyOptions
                );
            } else {
                deferred.resolve({ Successful: true, AccountIds: [accountId] });
            }
            
            
            return deferred.promise();
        },
        getAccountInfo: function (externalIds) {
            var self = this;
            var url = self.section.data("url");
            var tempData = {
                Accounts: [],
                Contacts: [],
                errorCount: 0
            };
           
            var requests = [];

            Enumerable.From(externalIds).ForEach(function (id) {
                var def = $.Deferred();
                self.getProxy().send({ AccountNumber: id }
                        , url
                        , function (successful, response, errors) {
                            tempData.Accounts = tempData.Accounts.concat(response.Accounts);
                            tempData.Contacts = tempData.Contacts.concat(response.Contacts);
                            if (response.Successful === false && response.ErrorCode >= 408) {
                                tempData.errorCount++;
                            }
                            def.resolve(tempData);
                        }
                        , self.proxyOptions);

                requests.push(def);
                    
            });
            
            return $.when.apply($, requests).done(function (data) {
                return data;
            });
            
           
        },               
        getScreen: function (id) {
            /// <summary>
            /// Get a reference to the screen object matching the specified id.
            /// </summary>
            /// <param name="id"></param>
            ///<returns type="Screen">the found screen object or null.</returns>
            var self = this;
            if (typeof id === "string") {
                return self.screens.find(function (x) {
                    return x.screenId === id;
                });
            }

            return self.screens.find(function (x) {
                return UL.CustomerManager.ScreenTypes[x.screenId] === id;
            });

        },
        loadScreens: function (selector) {
            /// <summary>
            /// Loads the screens matching the specified group selector.
            /// </summary>
            /// <param name="selector" type="String">The jquery selector string.</param>
            /// <returns type="Screen[]">Array of screens.</returns>

            var self = this;
            var screenList = [];
            var i;
            var screenObj;
            var templateItem;
            var container;
            var scr;

            var jqScreens = self.section.find(selector);
            for (i = 0; i < jqScreens.length; i++) {

                screenObj = $(jqScreens[i]);
                templateItem = screenObj.find('.item-template');
                container = templateItem.parent();
                container.empty();
                screenObj.remove();
                screenObj.show();

                //map info into screen obj
                scr = new Screen();
                scr.screenRef = $(jqScreens[i]);
                scr.screenId = screenObj.data("screenId");
                scr.itemContainer = container;
                scr.itemTemplateHtml = templateItem.htmlAll();
                screenList.push(scr);
            }

            return screenList;
        },
        registerLookupEvent: function () {
            /// <summary>
            /// Registers lookup button click event handler.
            /// </summary>

            var self = this;

            // disabling, or enabling "Sync from Customer Master" button.
            // As per story 2316. We are not going to disable button. 

            //if (self.isLineSelected) {
            //    self.disableSyncBtn();
            //}
            //else {
            //    self.enableSyncBtn();
            //}

            if (self.syncBtn) {
                self.syncBtn.click(function (e, evt) {
                    e.preventDefault();
                    self.sync();
                    return false;
                });
            }
        },
        sync: function () {
            /// <summary>
            /// Syncronizes the choices from the wizard selection dialog to the project edit form dialog.
            /// </summary>

            var self = this;                    
            self.state.currentScreen = self.getScreen(UL.CustomerManager.ScreenTypes.Accounts);
            var screen = self.state.currentScreen;

            if (!self.state.accountNumber() && !self.state.companyId) {
                UL.ShowAlert("Sync Not Available", "A Oracle/External Id must be manually entered to start a company sync.");
                return;
            }

            self.startBlocking();
            $.when(self.getAccountIds()).then(function (accountIdsResponse) {

                if (accountIdsResponse.Successful === false) {
                    self.stopBlocking();
                    UL.ShowAlert("Sync Not Available", "Unable to connect to server.");

                } else {
                    self.getAccountInfo(accountIdsResponse.AccountIds).done(function (data) {
                        self.stopBlocking();
                        if (data && data.Accounts && data.Contacts) {
                            self.accountList = data.Accounts;
                            self.contactList = data.Contacts;

                            if (data.errorCount > 0 && data.Accounts.length <= 0 && data.Contacts.length <= 0) {
                                UL.ShowAlert("Sync Not Available", "Unable to connect to server.");
                                return;
                            }

                            if (data.Accounts.length <= 0 && data.Contacts.length <= 0) {
                                UL.ShowAlert("Sync Not Available", "No accounts or contacts were found that match this project’s company.");
                                return;
                            }

                            if (data.Accounts.length > 1 && !self.isFixedAccount) {
                                //use modal wizard process more than one account
                                self.showScreen(screen, data.Accounts);
                            } else {
                                //if only one account and not fixed, set it and get contact
                                if (!self.isFixedAccount) {
                                    self.state.accountId = data.Accounts[0].Id;
                                    self.state.account = data.Accounts[0];
                                } else {
                                    self.state.accountId = self.fixedAccountId;
                                }

                                self.state.currentScreen = self.getScreen(UL.CustomerManager.ScreenTypes.Contacts);
                                screen = self.state.currentScreen;

                                if (data.Contacts.length > 1) {
                                    self.showScreen(screen, data.Contacts);
                                } else if (data.Contacts.length === 1) {
                                    //only one account set it and get contact
                                    self.state.contactId = data.Contacts[0].Id;
                                    self.state.contact = data.Contacts[0];
                                    self.updateForm(self.state);
                                } else {
                                    self.updateForm(self.state);
                                }
                            }
                        }
                    });
                }
            });
           
        },
        updateForm: function (state) {
            /// <summary>
            /// Updates the form using auto mapping code
            /// </summary>
            /// <param name="state"></param>

            var parentContext = this.opt.fieldGroupSelector;

            var autoMapFunc = function (dataObj) {

                Enumerable.From(dataObj).ForEach(function (member) {
                    var field = $(parentContext + ' [id$= _' + member.Key + ']');
                    if (field.length === 1) {
                        field.val(dataObj[member.Key]);
                    }

                });
            };

            autoMapFunc(state.account);
            autoMapFunc(state.contact);
        },
        getScreenData: function (screenId) {
            /// <summary>
            /// Gets the selected data from the screen matching the specified id.
            /// </summary>
            /// <param name="screenId" type="Int">The id of the screen.</param>
            /// <returns type="Object">Selection data.</returns>

            var self = this;
            switch (screenId) {
                case UL.CustomerManager.ScreenTypes.Accounts:
                    return self.accountList;
                case UL.CustomerManager.ScreenTypes.Contacts:
                    return self.contactList.where(function (x) { return x.AccountNumber === self.state.accountId; });
            }
        },
        enableSyncBtn: function () {
            /// <summary>
            /// Enables the syncrhronize button.
            /// </summary>

            this.syncBtn.prop("disabled", false);
        },
        disableSyncBtn: function () {
            /// <summary>
            /// Disables the Syncrhronize button.
            /// </summary>

            this.syncBtn.prop("disabled", true);
        },
        getModal: function () {
            /// <summary>
            /// Gets a reference to the jqResult containing the html element of the 
            /// bootstrap modal which contains the customer lookup wizard.
            /// </summary>
            /// <returns type="jqResultBootstrapModal">The model</returns>

            var self = this;

            if (self.modelRef === null) {
                self.modelRef = $('<div class="modal hide"></div>');
                self.modelRef.append(self.modalBaseContent.clone())
                    .on('hidden', function () {
                        $(this).data('modal', null);
                        self.modelRef.empty();
                        self.modelRef = null;
                    })
                    .on('shown', function () {
                        $(this).css("width", "450px")
                        .css("margin-left", "-122px")
                        .css("top", "175px");

                        self.modelRef.find(".next-btn").click(function (e) {
                            e.preventDefault();
                            var screenVal = UL.CustomerManager.ScreenTypes.getVal((self.state.currentScreen.screenId));
                            screenVal++;
                            var screenName = UL.CustomerManager.ScreenTypes.getName(screenVal);
                            self.state.currentScreen = self.getScreen(screenName);
                            var screen = self.state.currentScreen;
                            self.showScreen(screen, self.getScreenData(screenVal));
                            return false;

                        });

                        if (!self.isFixedAccount) {
                            self.modelRef.find(".back-btn").click(function (e) {
                                e.preventDefault();
                                var screenVal = UL.CustomerManager.ScreenTypes.getVal((self.state.currentScreen.screenId));
                                screenVal--;
                                var screenName = UL.CustomerManager.ScreenTypes.getName(screenVal);
                                self.state.currentScreen = self.getScreen(screenName);
                                var screen = self.state.currentScreen;
                                self.showScreen(screen, self.getScreenData(screenVal));
                                return false;

                            });
                        }

                        self.modelRef.find(".done-btn").click(function (e) {
                            e.preventDefault();
                            self.updateForm(self.state);
                            return true;

                        });
                    }).modal();
            }

            return self.modelRef;
        },
        showLoadAnimation: function () {
            /// <summary>
            /// Presents the loading animation.
            /// </summary>

            var self = this;
            if (self.progressAnimation !== null) {
                self.section.append(self.progressAnimation);
            }
        },
        hideLoadAnimation: function () {
            /// <summary>
            /// Hides the loading animation.
            /// </summary>

            var self = this;
            if (self.progressAnimation !== null) {
                self.progressAnimation.remove();
            }
        },
        startBlocking: function () {
            var blockOptions = {
                message: UL.loadAnimationLarge,
                baseZ: 11000,
                overlayCSS: {
                    backgroundColor: '#000',
                    opacity: 0.5,
                    cursor: 'wait'
                },
                css: {
                    border: 'none',
                    backgroundColor: 'transparent'
                },
                centerX: false,
                centerY: false
            };

            $('body').block(blockOptions);
        },
        stopBlocking: function () {
            $('body').unblock();
        },
   
        destroy: function () {
            return;
        }

    };


}(jQuery));