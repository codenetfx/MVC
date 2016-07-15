/// <reference path="../_references.js" />
/// <reference path="../../Lib/knockout/knockout-3.1.0.debug.js" />
/// <reference path="../../Lib/knockout/knockout.mapping-latest.debug.js" />
/// <reference path="../../Lib/jquery/plugins/jquery.blockUI.js" />
/// <reference path="../_ULReferences.js" />


(function ($) {
    'use strict';
    if (!window.UL) {
        window.UL = {};
    }

    UL.ProxyOptions = function () {
        /// <summary>
        /// Provides a a structured options class for the Proxy Class.
        /// </summary>
        ///<field name="enableLoadAnamation" type="Boolean">Enables a load anamation over the specified block element. if true, show load anamation, 
        /// otherwise hide anamation.</field>      
        ///<field name="enableKo" type="Boolean">Enables automatic serializtion and deserialization of the model to/from a knockout observable.</field>      
        ///<field name="enableBlocking" type="Boolean">if true, blocks all user interaction with page during requests, else user interaction 
        /// is still possible.</field>      
        ///<field name="blockedElement" type="JQElement">a refernece to a jqElement that will be blocked when enableBlocking is true. 
        /// If reference is null or not not found, whole page will be blocked when enable blocking is true.</field>      
        ///<field name="transparentBlock" type="Boolean">If true, block overlay will be transparent, otherwise will use default color setting.</field>     
        this.loadAnamationRef = null;
        this.enableLoadAnamation = true;
        this.enableKo = false;
        this.enableBlocking = true;
        this.blockedElement = null;
        this.transparentBlock = false;
	    this.method = 'POST';
    };

    UL.Proxy = function (initOptions) {
        /// <summary>
        /// Provides a centralized ajax communications class that manages the UX anamantions, blocking, 
        /// and error handling associated with a server call.
        /// </summary>
        /// <param name="initOptions" type=" UL.ProxyOptions" optional="true">Provides operational options to be specified.</param>

        var opt = (initOptions && (initOptions instanceof UL.ProxyOptions))
            ? initOptions : new UL.ProxyOptions();

        this.getOptions = function () {
            /// <summary>
            /// Reterns a muted copy of the initialization options.
            /// </summary>
            /// <returns type="UL.ProxyOptions"></returns>
            return opt; //.deepCopy();
        };      
    };

    UL.Proxy.prototype = {

        send: function (model, url, callback, overrideOptions) {
            /// <summary>
            /// Sends the specified model to the specifled controller                       
            /// </summary>
            /// <param name="model">The model to be sent to the specified url</param>
            /// <param name="url" type="String">The url to call</param>
            /// <param name="callback" optonal="true" type="function">a function that will be call when the comunication is comleted.
            /// Method signature is function(bool successful, object dataRef, object errorDetails) </param>
            /// <param name="overideOptions" optional="true">Provides a option object that will 
        	/// override the default options specified when the proxy was instanciated.</param>

            var self = this;
            var callOptions = this.initRequest(overrideOptions);
            var transmitData = (callOptions.enableKo)
                ? ko.mapping.toJSON(model)
                : model;

            $.ajax({
                url: url,
                type: callOptions.method,
                data: JSON.stringify(transmitData),
                dataType: "json",
                contentType: "application/json; charset=utf-8",
                success: function (data) {

                    if (callback && typeof callback === 'function') {
                        if (callOptions.enableKo) {
                            callback(ko.mapping.fromJS(data, model));
                        }
                        else {
                            callback(true, data);
                        }
                    }
                },
                error: function (jqXHR, textStatus, errorThrown) {
                    //alert is temporary need to replace for full blown ux error handling
                    //window.alert("There was an error posting the data to the server: " + error.responseText);
                    UL.HandleAjaxUnhandledError(jqXHR, textStatus, errorThrown);
                    callback(false, null, jqXHR);
                },
                complete: function () {
                    self.tearDownRequest(callOptions);
                }
            });
        },
        initRequest: function (override) {
            /// <summary>
            /// Initializes the stage of the request in the form
            /// of call options and returns them.
            /// </summary>
            /// <param name="override"></param>
            /// <returns type="UL.ProxyOptions">The call specific options.</returns>
            var callOptions = this.getOptions();
            if (override) {
                if (override || (override instanceof UL.ProxyOptions)) {
                    jQuery.extend(callOptions, override);
                }
            }

            this.startBlocking(callOptions);
            return callOptions;
        },

        tearDownRequest: function (callOptions) {
            /// <summary>
            /// Shuts down a request features 
            /// that may have init prior to the request
            /// </summary>
            this.stopBlocking(callOptions);
        },
        stopBlocking: function (callOptions) {
            /// <summary>
            /// Stops the screen or element from being block.
            /// </summary>
            var blockedElem = this.getElementToBlock(callOptions);
            if (blockedElem !== null) {
                blockedElem.unblock();
            }
            else {
                $.unblockUI();
            }
        },
        startBlocking: function (options) {
            /// <summary>
            /// Starts blocking user interactions based on the options.
            /// </summary>
            /// <param name="options"></param>
            if (options.enableBlocking) {

                var blockedElem = this.getElementToBlock(options);

                var blockOptions = {};

                if(options.enableLoadAnamation)
                {
                    if(options.loadAnamationRef){
                        blockOptions.message = options.loadAnamationRef[0];
                    }
                    else if (blockedElem) {
                        blockOptions.message = UL.loadAnamationSmall[0];                        
                    }
                    else {
                        blockOptions.message = UL.loadAnimationLarge[0];
                    }

                    blockOptions.css = {
                        border: 'none',                       
                        backgroundColor: 'transparent'                     
                    };

                    blockOptions.centerX = false;
                    blockOptions.centerY = false; 
                }              

                if (options.transparentBlock) {

                    blockOptions.overlayCSS = {
                        backgroundColor: '#000',
                        opacity: 0.0,
                        cursor: 'wait'
                    };
                }

                blockOptions.baseZ = 1500;
                if (blockedElem !== null) {
                    $(blockedElem).block(blockOptions);
                }
                else {
                    $.blockUI(blockOptions);
                }
            }
        },
        getElementToBlock: function (options) {
            /// <summary>
            /// returns the element to be blocked during a request.
            /// </summary>
            /// <param name="options"></param>
            /// <returns type=""></returns>
            return (options.blockedElement !== null && options.blockedElement.length > 0)
                        ? options.blockedElement
                        : null;
        }
    };




}(jQuery));